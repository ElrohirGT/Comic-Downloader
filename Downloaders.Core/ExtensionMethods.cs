using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Downloaders.Core
{
    public static class ExtensionMethods
    {
        /// <summary>
        /// Iterates over each element in the <paramref name="source"/>.
        /// Where each execution may run in parallel and in an async manner.
        /// </summary>
        /// <typeparam name="T">The type of the argument that will be passed to each execution.</typeparam>
        /// <param name="source">The <see cref="IEnumerable{T}"/> to loop through</param>
        /// <param name="body">The action that will be executed for each item in the <paramref name="source"/>.</param>
        /// <param name="maxDegreeOfParallelism">The maximum of executions that will be done concurrently.</param>
        /// <param name="scheduler">A custom task scheduler, if this is null the default will be used.</param>
        /// <returns>A task that completes once all executions have finished for each element.</returns>
        public static Task ForEachParallelAsync<T>(this IEnumerable<T> source, Func<T, Task> body, int maxDegreeOfParallelism = DataflowBlockOptions.Unbounded, TaskScheduler? scheduler = null)
        {
            var options = new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = maxDegreeOfParallelism
            };
            if (scheduler is not null)
                options.TaskScheduler = scheduler;

            var block = new ActionBlock<T>(body, options);

            foreach (var item in source)
                block.Post(item);

            block.Complete();
            return block.Completion;
        }

        /// <summary>
        /// Iterates over each element in the <paramref name="source"/>.
        /// Where each execution may run in parallel and in an async manner.
        /// </summary>
        /// <typeparam name="T">The type of the argument that will be passed to each execution.</typeparam>
        /// <param name="source">The <see cref="IAsyncEnumerable{T}"/> to loop through</param>
        /// <param name="body">The action that will be executed for each item in the <paramref name="source"/>.</param>
        /// <param name="maxDegreeOfParallelism">The maximum of executions that will be done concurrently.</param>
        /// <param name="scheduler">A custom task scheduler, if this is null the default will be used.</param>
        /// <returns>A task that completes once all executions have finished for each element.</returns>
        public static async Task ForEachParallelAsync<T>(this IAsyncEnumerable<T> source, Func<T, Task> body, int maxDegreeOfParallelism = DataflowBlockOptions.Unbounded, TaskScheduler? scheduler = null)
        {
            var options = new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = maxDegreeOfParallelism
            };
            if (scheduler is not null)
                options.TaskScheduler = scheduler;

            var block = new ActionBlock<T>(body, options);

            await foreach (var item in source)
                block.Post(item);

            block.Complete();
            await block.Completion.ConfigureAwait(false);
        }

        /// <summary>
        /// Iterates over each element in the <paramref name="source"/>.
        /// Where each execution may run in parallel and in an async manner.
        /// It also provides the index of the element that is being iterated over.
        /// </summary>
        /// <typeparam name="T">The type of the argument that will be passed to each execution.</typeparam>
        /// <param name="source">The <see cref="IEnumerable{T}"/> to loop through</param>
        /// <param name="body">The action that will be executed for each item in the <paramref name="source"/>.</param>
        /// <param name="maxDegreeOfParallelism">The maximum of executions that will be done concurrently.</param>
        /// <param name="scheduler">A custom task scheduler, if this is null the default will be used.</param>
        /// <returns>A task that completes once all executions have finished for each element.</returns>
        public static Task ForParallelAsync<T>(this IEnumerable<T> source, Func<int, T, Task> body, int maxDegreeOfParallelism = DataflowBlockOptions.Unbounded, TaskScheduler? scheduler = null)
        {
            ExecutionDataflowBlockOptions options = new() { MaxDegreeOfParallelism = maxDegreeOfParallelism };
            if (scheduler is not null)
                options.TaskScheduler = scheduler;

            var block = new ActionBlock<(int Index, T Value)>(tuple => body.Invoke(tuple.Index, tuple.Value));

            var enumerator = source.GetEnumerator();
            int index = 0;
            while (enumerator.MoveNext())
            {
                block.Post((index, enumerator.Current));
                ++index;
            }

            block.Complete();
            return block.Completion;
        }
    }
}