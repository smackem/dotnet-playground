using System;
using System.Linq;
using System.Activities;
using System.Activities.Statements;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GuessGame
{
    class Program
    {
        static void Main(string[] args)
        {
            var random = new Random();
            var arguments = new Dictionary<string, object>
            {
                { "Number", random.Next(100) },
            };

            var vars = WorkflowInvoker.Invoke(new StateMachine(), arguments);

            foreach (var kvp in vars)
                Console.WriteLine($"{kvp.Key}: {kvp.Value}");

            Console.Write("Enter to quit...");
            Console.ReadLine();
        }
    }

    static class WorkflowInvokerExtensions
    {
        public static Task<IDictionary<string, object>> InvokeTaskAsync(this WorkflowInvoker invoker)
        {
            var tcs = new TaskCompletionSource<IDictionary<string, object>>();

            invoker.BeginInvoke(asyncResult =>
            {
                IDictionary<string, object> result = null;
                bool thrown;

                try
                {
                    result = invoker.EndInvoke(asyncResult);
                    thrown = false;
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                    thrown = true;
                }

                if (thrown == false)
                    tcs.SetResult(result);
            }, null);

            return tcs.Task;
        }
    }
}
