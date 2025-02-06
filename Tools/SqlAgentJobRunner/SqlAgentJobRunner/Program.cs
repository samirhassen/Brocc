using System;
using System.Threading;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Smo.Agent;

namespace SqlAgentJobRunner
{
    public class Program
    {
        static void Main(string[] args)
        {
            var shouldStart = Convert.ToBoolean(args[0]);
            var serverName = args[1];
            var jobName = args[2];
            var connStr = args.Length >= 4 ? args[3] : null;
            var waitForCompletion = false;

            if (args?.Length == 5)
            {
                waitForCompletion = Convert.ToBoolean(args[4]);
            }

            Console.WriteLine($"shouldStart {shouldStart}");
            Console.WriteLine($"jobName {jobName}");
            Console.WriteLine($"serverName {serverName}");
            Console.WriteLine($"waitForCompletion {waitForCompletion}");

            var server = new Server(serverName);
            try
            {
                if (serverName == "localhost")
                {
                    // Integrated Security on localhost, otherwise connect with pw
                    server.ConnectionContext.LoginSecure = true;
                }
                else
                {
                    // Our environments does not currently (nov 2021) use AAD-accounts for app pool identity 
                    // connected to our SQL MI. 
                    server.ConnectionContext.LoginSecure = false;
                    server.ConnectionContext.ConnectionString = connStr;
                }

                server.ConnectionContext.Connect();
                var job = server.JobServer.Jobs[jobName];
                if (job.CurrentRunStatus == JobExecutionStatus.Idle && shouldStart)
                {
                    job.Start();
                    Console.WriteLine($"Agent job {jobName} started. ");
                    Thread.Sleep(1000); // Need to wait before we get status below. 
                }

                job.Refresh();
                var jobStatus = job.CurrentRunStatus;
                if (waitForCompletion)
                {
                    while (jobStatus != JobExecutionStatus.Idle)
                    {
                        Thread.Sleep(2000);
                        job.Refresh();
                        jobStatus = job.CurrentRunStatus;
                        Console.WriteLine($"Job status: {job.CurrentRunStatus}");
                    }
                }

                if (jobStatus == JobExecutionStatus.Executing)
                {
                    ExitWithComment("Job is executing, exiting with status 1. ", 1);
                }
                if (jobStatus == JobExecutionStatus.Idle)
                {
                    if(job.LastRunOutcome == CompletionResult.Failed)
                        ExitWithComment("Job failed, exiting with status 5. ", 5);
                    else
                        ExitWithComment("Job is idle (done), exiting with status 0. ", 0);
                }

            }
            catch (Exception ex)
            {
                ExitWithComment($"ERROR: {ex}", 5);
            }
            finally
            {
                if (server.ConnectionContext.IsOpen)
                {
                    server.ConnectionContext.Disconnect();
                    ExitWithComment("All done, disconnected from SQL Agent. ", 0);
                }

            }

        }

        private static void ExitWithComment(string comment, int exitCode)
        {
            Console.WriteLine(comment);
            Environment.Exit(exitCode);
        }
    }
}
