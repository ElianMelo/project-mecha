using System.Linq;

// One action catalog supplies descriptions to both settings and the tutorial.
public static class ControlTutorialText
{
    public static string Build()
    {
        var settings = GameControlBindings.Settings;
        if (settings == null) return string.Empty;
        bool controller = GameInputPresentation.UsingGamepad;
        var lines = settings.descriptions.Where(d => d.tutorial).Select(d => GameText.Get("Input", "tutorial.action",
            GameControlBindings.Hint(d.action, controller), GameText.Get(d.title)));
        return string.Join("   •   ", lines) + "\n" +
            GameText.Get("Input", "tutorial.chord", GameInputPresentation.Hint("Dash"), GameInputPresentation.Hint("Jump"), GameText.Get("Combat", "term.Rocket")) + "   •   " +
            GameText.Get("Input", "tutorial.chord", GameInputPresentation.Hint("Down"), GameInputPresentation.Hint("Attack"), GameText.Get("Input", "action.dive")) + "\n" +
            GameText.Get("Input", "tutorial.hold", GameInputPresentation.Hint("Attack"), GameText.Get("Input", "action.portal")) + "   •   " +
            GameText.Get("Input", "tutorial.then", GameText.Get("Combat", "term.Rocket"), GameInputPresentation.Hint("Down"), GameText.Get("Combat", "term.Mushidon"));
    }
}
