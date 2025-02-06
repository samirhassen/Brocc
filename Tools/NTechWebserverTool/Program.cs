// See https://aka.ms/new-console-template for more information
using Microsoft.Web.Administration;
/*
* Grant read permission to the user running that to
%windows%\system32\inetsrv\config.
*/

if (args.Length < 2)
{
    Console.WriteLine("usage: NTechWebserverTool.exe [start-pool|stop-pool] [pool-name-prefix or * for all] [wait-seconds]");
    return;
}

OperationCode operation;

if (args[0] == "start-pool")
{
    operation = OperationCode.Start;
}
else if (args[0] == "stop-pool")
{
    operation = OperationCode.Stop;
}
else
{
    throw new Exception("Must be start-pool or stop-pool or restart-pool");
}

int waitSeconds = 30;
if (args.Length > 2)
{
    waitSeconds = int.Parse(args[2]);
}

try
{

    var serverManager = new ServerManager();
    List<ApplicationPool> pools;
    string pattern = args[1];
    if (pattern == "*")
    {
        pools = serverManager.ApplicationPools.ToList();
    }
    else
    {
        pools = serverManager.ApplicationPools.Where(x => x.Name.StartsWith(pattern)).ToList();
    }

    if (pools.Count == 0)
    {
        Console.WriteLine($"Pools {pattern} do not exist");
        return;
    }

    bool anyPending = false;
    void WaitIfPending(ObjectState state)
    {
        if (state == ObjectState.Starting || state == ObjectState.Stopping)
        {
            anyPending = true;
        }
    }

    foreach (var appPool in pools)
    {
        if (operation == OperationCode.Stop || operation == OperationCode.Restart)
        {
            if (appPool.State == ObjectState.Started)
            {
                WaitIfPending(appPool.Stop());
            }
        }
        if (operation == OperationCode.Start)
        {
            if (appPool.State == ObjectState.Stopped)
            {
                WaitIfPending(appPool.Start());
            }
        }
    }

    if (anyPending)
    {
        Console.WriteLine("Waiting for start/stop to finish");
        Thread.Sleep(waitSeconds * 1000);
    }

    Console.WriteLine("Done");
}
catch (UnauthorizedAccessException ex)
{
    if (ex.Message.Contains("Cannot read configuration file due to insufficient permissions"))
    {
        throw new Exception($"You need to grant read permission to '{Environment.UserName}' for '%windows%\\system32\\inetsrv\\config'");
    }
    else
    {
        throw;
    }
}




enum OperationCode
{
    Start,
    Stop,
    Restart
}
