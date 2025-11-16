namespace BlazorApp.Game.Battle
{
    public interface IBattleIntent { }

    public record TestInputIntent(int TestId) : IBattleIntent;
    public record ReserveAttackIntent(AttackType Attack) : IBattleIntent;
    public record ChooseResponseIntent(ResponseType Response) : IBattleIntent;
    public record ConfirmInputTapIntent() : IBattleIntent;
    public record ConfirmResolutionIntent() : IBattleIntent;
    public record CharacterTapIntent(int CharacterId) : IBattleIntent;
}
