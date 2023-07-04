using Rowbot.Common.Extensions;
using System.Reflection;

namespace Rowbot.Framework.Blocks
{
    internal sealed class BlockLinker
    {
        /// <summary>
        /// Links a series of blocks.
        /// </summary>
        public Stack<List<Func<Task>>> LinkBlocks(PriorityQueue<IBlock, int> blockQueue, BlockContext context)
        {
            if (blockQueue.Count <= 2)
            {
                throw new BlockBuilderException($"Too few blocks");
            }

            Stack<List<Func<Task>>> pipelineTasks = new();
            List<Func<Task>> currentSequence = new();

            var queueCount = blockQueue.Count;

            IBlock previousBlock = blockQueue.Dequeue();
            IBlock nextBlock = blockQueue.Dequeue();

            for (var index = 1; index < queueCount; index++)
            {
                if (previousBlock.GetType().ImplementsGenericInterface(typeof(IBlockTarget<>)) &&
                    nextBlock.GetType().ImplementsGenericInterface(typeof(IBlockSource<>)))
                {
                    try
                    {
                        GetLinkToMethod(nextBlock, index)
                            .Invoke(nextBlock, new object[] { previousBlock });

                        currentSequence.Add(previousBlock.PrepareTask(context));
                    }
                    catch (BlockBuilderException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        throw new BlockBuilderException($"Cannot link previous block of type {nextBlock.GetType()} to next block of type {nextBlock.GetType()}", ex);
                    }
                }
                else
                {
                    currentSequence.Add(previousBlock.PrepareTask(context));
                    pipelineTasks.Push(currentSequence);
                    currentSequence = new List<Func<Task>>();
                }

                previousBlock = nextBlock;

                if (blockQueue.Count > 0)
                {
                    nextBlock = blockQueue.Dequeue();
                }
                else
                {
                    currentSequence.Add(nextBlock.PrepareTask(context));
                    pipelineTasks.Push(currentSequence);
                }
            }

            return pipelineTasks;
        }

        private MethodInfo GetLinkToMethod(IBlock block, int index)
        {
            var blockType = block
                .GetType()
                .GetInterfaces()
                .FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IBlockSource<>));
            if (blockType is null)
            {
                throw new BlockBuilderException($"Block at index {index} does not implement IBlockSource<>");
            }

            var linkToMethod = typeof(IBlockSource<>)
                .MakeGenericType(blockType.GetGenericArguments().Last())
                .GetMethod("LinkTo");
            if (linkToMethod is null)
            {
                throw new BlockBuilderException($"Block at index {index} does not implement LinkTo method");
            }

            return linkToMethod;
        }
    }
}