using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine.Localization.Tables;
using UnityPipeline.Microsoft.CodeAnalysis;
using UnityPipeline.Microsoft.CodeAnalysis.CSharp;
using UnityPipeline.Microsoft.CodeAnalysis.CSharp.Syntax;

public static class LocalizationSourceMigration
{
    private static readonly Dictionary<string, StringTableCollection> tables = new Dictionary<string, StringTableCollection>();
    public static string Migrate()
    {
        int count = 0;
        foreach (string path in Directory.GetFiles("Assets/_Project/Scripts/Interface", "*.cs").Concat(new[]{"Assets/_Project/Scripts/System/MechaLobbyService.cs"}))
        {
            string file = Path.GetFileNameWithoutExtension(path);
            if (!new[]{"AudioConfigurationMenu","ChatHistory","ComboConfigurator","ControllerTextEntry","GameInputPresentation","GameplayDiagnostics","LevelObjectiveHud","LobbyPlayerCard","MechaChat","MechaGameplayHud","MechaLobbyPanel","RouteMapBoard","RouteMapNodeView","SkillFocusPresentation","VideoConfigurationMenu","MechaLobbyService"}.Contains(file)) continue;
            string category = file.Contains("Lobby") ? "Lobby" : file.Contains("Route") ? "Routing" : file.Contains("Video") || file.Contains("Audio") ? "Settings" : file.Contains("Chat") ? "Chat" : file.Contains("Controller") || file.Contains("Input") ? "Input" : file.Contains("Diagnostics") ? "Diagnostics" : file.Contains("Combo") || file.Contains("Skill") ? "Combat" : "HUD";
            var rewriter = new Rewriter(file, category);
            var root = CSharpSyntaxTree.ParseText(File.ReadAllText(path), new CSharpParseOptions(preprocessorSymbols: new[]{"UNITY_EDITOR"})).GetRoot();
            var changed = rewriter.Visit(root);
            if (rewriter.Count > 0) File.WriteAllText(path, changed.ToFullString());
            count += rewriter.Count;
        }
        foreach (var collection in tables.Values)
        {
            EditorUtility.SetDirty(collection); EditorUtility.SetDirty(collection.SharedData);
            foreach (var table in collection.StringTables) EditorUtility.SetDirty(table);
            LocalizationEditorSettings.EditorEvents.RaiseCollectionModified(null, collection);
        }
        AssetDatabase.SaveAssets();
        return "Migrated " + count + " source expressions";
    }

    private sealed class Rewriter : CSharpSyntaxRewriter
    {
        private readonly string file, category;
        public int Count;
        public Rewriter(string file, string category) { this.file = file; this.category = category; }
        private bool Eligible(SyntaxNode node)
        {
            if (node.Ancestors().Any(n => n is AttributeSyntax)) return false;
            if (node.Parent is BinaryExpressionSyntax binary && (binary.IsKind(SyntaxKind.EqualsExpression) || binary.IsKind(SyntaxKind.NotEqualsExpression))) return false;
            var call = node.Ancestors().OfType<InvocationExpressionSyntax>().FirstOrDefault();
            string method = call?.Expression.ToString() ?? "";
            if (method.StartsWith("GameText.") || method.Contains("PlayerPrefs") || method.EndsWith("Hint") || method.EndsWith("Replace") || method.EndsWith("SetTrigger") || method.EndsWith("SetBool") || method.EndsWith("HasProperty") || method.EndsWith("Read") || method.Contains("Debug.") || method.EndsWith("ToString")) return false;
            if (method.Contains("Undo.") || method.Contains("AssetDatabase.") || method.Contains("LocalizedString")) return false;
            if (node.Ancestors().Any(n => n is ObjectCreationExpressionSyntax obj && obj.Type.ToString() == "GameObject")) return false;
            string value = node is LiteralExpressionSyntax literal ? literal.Token.ValueText : node.ToString();
            if (!value.Any(char.IsLetter)) return false;
            if (new[]{"SkillA","SkillS","SkillD","<color=#",">P","</color> ","Mecha.Video."}.Contains(value)) return false;
            if (file == "GameInputPresentation" && node.GetLocation().GetLineSpan().StartLinePosition.Line == 40) return false;
            if (file == "GameplayDiagnostics" && value == "NEUTRAL") return false;
            if (file == "MechaLobbyService") return value.Contains(" ") || node is InterpolatedStringExpressionSyntax;
            return true;
        }
        private ExpressionSyntax Reference(string value, IEnumerable<string> arguments = null)
        {
            if (!tables.TryGetValue(category, out var collection))
            {
                collection = LocalizationEditorSettings.GetStringTableCollection(category) ?? LocalizationEditorSettings.CreateStringTableCollection(category, "Assets/_Project/Localization/Strings/" + category);
                tables[category] = collection;
            }
            string words = Regex.Replace(value, @"\{[^}]*\}|<[^>]*>", " ");
            string hint = string.Join(" ", Regex.Matches(words, "[A-Za-z]+").Cast<Match>().Take(3).Select(match => match.Value));
            string key = MechaLocalizationAuthoring.NewKey(category, string.IsNullOrEmpty(hint) ? file + " value" : hint);
            var table = collection.StringTables.Single(t => t.LocaleIdentifier.Code == "en");
            table.AddEntry(key, value);
            Count++;
            var args = arguments?.ToArray() ?? Array.Empty<string>();
            return SyntaxFactory.ParseExpression("GameText.Get(" + Quote(category) + ", " + Quote(key) + (args.Length > 0 ? ", " + string.Join(", ", args) : "") + ")");
        }
        private static string Quote(string value) => SyntaxFactory.Literal(value).ToString();
        public override SyntaxNode VisitLiteralExpression(LiteralExpressionSyntax node) => node.IsKind(SyntaxKind.StringLiteralExpression) && Eligible(node) ? Reference(node.Token.ValueText).WithTriviaFrom(node) : base.VisitLiteralExpression(node);
        public override SyntaxNode VisitInterpolatedStringExpression(InterpolatedStringExpressionSyntax node)
        {
            if (!Eligible(node)) return base.VisitInterpolatedStringExpression(node);
            var text = new StringBuilder(); var args = new List<string>();
            foreach (var content in node.Contents)
            {
                if (content is InterpolatedStringTextSyntax part) text.Append(part.TextToken.ValueText);
                else if (content is InterpolationSyntax hole)
                {
                    text.Append('{').Append(args.Count);
                    if (hole.AlignmentClause != null) text.Append(',').Append(hole.AlignmentClause.Value);
                    if (hole.FormatClause != null) text.Append(':').Append(hole.FormatClause.FormatStringToken.ValueText);
                    text.Append('}'); args.Add("(" + Visit(hole.Expression).ToString() + ")");
                }
            }
            return Reference(text.ToString(), args).WithTriviaFrom(node);
        }
    }
    public static string Inventory()
    {
        var rows = Directory.GetFiles("Assets/_Project/Scripts", "*.cs", SearchOption.AllDirectories)
            .SelectMany(path => CSharpSyntaxTree.ParseText(File.ReadAllText(path)).GetRoot().DescendantNodes()
                .Where(n => (n is LiteralExpressionSyntax l && l.IsKind(SyntaxKind.StringLiteralExpression) || n is InterpolatedStringExpressionSyntax)
                    && !n.Ancestors().Any(a => a is AttributeSyntax || a is InterpolatedStringExpressionSyntax))
                .Select(n => path.Replace('\\','/') + "\t" + (n.GetLocation().GetLineSpan().StartLinePosition.Line + 1) + "\t" + n.ToString()));
        File.WriteAllLines("Logs/LocalizationLiterals.tsv", rows);
        return "Inventory written";
    }
}

