namespace Rowbot.Framework.Blocks
{
    public interface IBlock
    {
        Func<Task> PrepareTask(BlockContext context);
    }
}
