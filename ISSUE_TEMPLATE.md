DocFX Version:

### Title
*A short description of the bug that becomes the issue title*  
e.g. Long polling transport tries reconnecting forever when ping succeeds but poll request fails.

### Functional impact
*Does the bug result in any actual functional issue, if so, what?*  
e.g. If poll request starts working again, it recovers. Otherwise, it sends many requests per sec to the server.

### Minimal repro steps
*What is the smallest, simplest set of steps to reproduce the issue. If needed, provide a project that demonstrates the issue.*  
e.g.
1. Enable SQL scale out in the samples app
2. Open the Raw connection sample page with long polling transport (~/Raw/?transport=longPolling)
3. Confirm the connection is connected
4. Stop the SQL Server service
5. Wait for the connection to time out (~2 minutes)

### Expected result
*What would you expect to happen if there wasn't a bug?*  
e.g. The connection should stop trying to reconnect after the disconnect timeout (~30 seconds).

### Actual result
*What is actually happening?*  
e.g. The connection tries reconnecting forever.

### Further technical details
*Optional, details of the root cause if known*  
e.g. [This check](https://github.com/SignalR/SignalR/blob/dev/src/Microsoft.AspNet.SignalR.Client.JS/jquery.signalR.transports.longPolling.js#L149) returns true while reconnecting even if the ping succeeds and the poll fails, then the next line changes the state of the connection to connected, which cancels the reconnect timeout that tracks how long the reconnect phase is taking and eventually would forcibly stop the connection. The poll request from a few lines before eventually fails and the error handler starts the whole dance again by setting the connection back to the reconnecting state (restarting the reconnect timeout) and then doing the ping/poll check again. Around and around we go.
