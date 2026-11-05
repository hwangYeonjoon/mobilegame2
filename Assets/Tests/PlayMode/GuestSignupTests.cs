using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using ScrapGuard.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace ScrapGuard.Tests
{
    public class GuestSignupTests
    {
        [UnityTest] public IEnumerator LostSignupResponseCanResumeAfterRestartWithoutAnotherAccount()
        {
            string folder=Path.Combine(Path.GetTempPath(),"scrap-signup-"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            // A separate local save must survive server signup and recovery untouched.
            string localPath=Path.Combine(folder,"profile.json");File.WriteAllText(localPath,"existing local progress");
            var portProbe=new TcpListener(IPAddress.Loopback,0);portProbe.Start();
            int port=((IPEndPoint)portProbe.LocalEndpoint).Port;portProbe.Stop();
            string endpoint="http://127.0.0.1:"+port;
            string policy=JsonUtility.ToJson(new ReleasePolicy{latestBuild=AppRelease.Build,minimumBuild=AppRelease.MinimumBuild,latestVersion=AppRelease.Version,packageName=AppRelease.Package,combatRules=Combat.RulesVersion});
            string success=JsonUtility.ToJson(new ServerReply{success=true,playerId="one-account",revision=1,state=GameState.New()});
            string firstBody=null,secondBody=null,restoredBearer=null,persistedSession=null;
            int registrations=0,restorations=0;
            using(var listener=new HttpListener())
            {
                listener.Prefixes.Add(endpoint+"/");listener.Start();
                Task server=Task.Run(async()=>{
                    for(int i=0;i<6;i++)
                    {
                        var context=await listener.GetContextAsync();string body=policy;
                        if(context.Request.Url.AbsolutePath=="/v1/auth/guest")
                        {
                            string request;using(var reader=new StreamReader(context.Request.InputStream))request=reader.ReadToEnd();
                            registrations++;
                            if(registrations==1)
                            {
                                firstBody=request;persistedSession=File.ReadAllText(Path.Combine(folder,"cloud-session.json"));
                                // Simulate commit followed by a failed gateway response.
                                context.Response.StatusCode=503;body="{}";
                            }
                            else {secondBody=request;body=success;}
                        }
                        else if(context.Request.Url.AbsolutePath=="/v1/bootstrap")
                        {restorations++;restoredBearer=context.Request.Headers["Authorization"];body=success;}
                        byte[] bytes=Encoding.UTF8.GetBytes(body);context.Response.ContentType="application/json";context.Response.ContentLength64=bytes.Length;
                        await context.Response.OutputStream.WriteAsync(bytes,0,bytes.Length);context.Response.Close();
                    }
                });
                try
                {
                    var first=new CloudClient(folder);first.Session.endpoint=endpoint;ServerReply reply=null;
                    yield return first.StartGuest(r=>reply=r);
                    Assert.IsFalse(reply.success);Assert.IsFalse(first.Session.enabled);
                    string secret=first.Session.secret;Assert.AreEqual(43,secret.Length);
                    Assert.IsTrue(JsonUtility.FromJson<CloudSession>(persistedSession).guestRegistrationPending);
                    Assert.AreEqual(secret,JsonUtility.FromJson<GuestRequest>(firstBody).secret);
                    var restarted=new CloudClient(folder);
                    Assert.IsTrue(restarted.Session.guestRegistrationPending);Assert.AreEqual(secret,restarted.Session.secret);
                    yield return restarted.StartGuest(r=>reply=r);
                    Assert.IsTrue(reply.success);Assert.IsTrue(restarted.Session.enabled);
                    Assert.AreEqual(firstBody,secondBody);Assert.IsFalse(restarted.Session.guestRegistrationPending);
                    restarted.Disconnect();var returning=new CloudClient(folder);
                    Assert.IsTrue(returning.Session.localPlaySelected);
                    yield return returning.StartGuest(r=>reply=r);
                    Assert.IsTrue(reply.success);Assert.IsFalse(returning.Session.localPlaySelected);
                    Assert.AreEqual("one-account",returning.Session.playerId);
                    Assert.AreEqual("Bearer "+secret,restoredBearer);
                    Assert.AreEqual(2,registrations);Assert.AreEqual(1,restorations);
                    Assert.AreEqual("existing local progress",File.ReadAllText(localPath));
                    Assert.IsTrue(server.IsCompleted);Assert.IsFalse(server.IsFaulted,server.Exception?.ToString());
                }
                finally {listener.Stop();if(Directory.Exists(folder))Directory.Delete(folder,true);}
            }
        }
    }
}
