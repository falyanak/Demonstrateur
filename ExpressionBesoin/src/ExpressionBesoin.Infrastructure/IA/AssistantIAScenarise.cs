using ExpressionBesoin.Application.Fiches;

namespace ExpressionBesoin.Infrastructure.IA;

/// <summary>
/// Adaptateur d'<see cref="IAssistantIA"/> sans réseau (DEB-Q41) : la démonstration reste jouable hors ligne le jour du
/// séminaire. Coquille sans décision : le scénario est <see cref="ScenarioDialogue.Repondre"/>.
/// </summary>
public sealed class AssistantIAScenarise : IAssistantIA
{
    public ValueTask<string> Repondre(ContexteTour contexte, CancellationToken annulation)
    {
        annulation.ThrowIfCancellationRequested();
        return new ValueTask<string>(ScenarioDialogue.Repondre(contexte));
    }
}
