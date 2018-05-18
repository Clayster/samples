using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using DXMPP;
using CDO;

namespace ClaysterSamples
{
	public class MainCDO
	{
		class TestCDO : IDisposable
		{
			Connection Uplink;
			CDOController CDOUplink;

			public enum Role
			{
				DataStorage,
				Reader,
				Writer
			}

			Role MyRole;
			static EventWaitHandle Quit = new EventWaitHandle(false, EventResetMode.ManualReset);

			public void Dispose()
			{
				Uplink.Dispose();
			}


			void SetClaimKey()
			{
				string claimkey = Guid.NewGuid().ToString();
				Console.WriteLine("Setting claimkey to: {0}", claimkey);
				var WaiterSetClaimKey = CDOUplink.SetClaimKey(claimkey);
				WaiterSetClaimKey.Wait();
				Console.WriteLine("Succes: {0}", WaiterSetClaimKey.Result);
			}

			void StartReadSession()
			{
				Console.WriteLine("Requesting session");
				ResourceAccess Res1 = new ResourceAccess()
				{
					ResourcePath = "readables/read1",
					Subordinates = false,
					Verbs = new List<DataVerb>() { DataVerb.GET }
				};
				List<ResourceAccess> Resources = new List<ResourceAccess>();
				Resources.Add(Res1);
				var Awaiter = CDOUplink.RequestSession("757df3d81c8645a69bc22a8f2576bd30",
				                                       Resources,
				                                       new TimeSpan(1, 0, 0));
				Awaiter.Wait();
				Console.WriteLine("Success: {0}", Awaiter.Result != null);
				if (Awaiter.Result != null)
				{
					Console.WriteLine("Session ends at {0}", Awaiter.Result.SessionEndsAtUTC);
				}
				else
				{
					Console.WriteLine("Aborting due to error");
					return;
				}

				Console.WriteLine("I will confirm the session");
				var Awaiter2 = CDOUplink.StartSession(Awaiter.Result.ID);
				Awaiter2.Wait();
				if (Awaiter2.Result != null)
				{
					Console.WriteLine("Session towards {0} started", Awaiter2.Result.ToString());
				}
			}

			void RegisterResources()
			{
				Console.WriteLine("Registering resources");

				Resource Res1 = new Resource()
				{
					Capabilities = new List<string>() { "urn:clayster:lwtsd" },
					Path = "readables/read1",
					SupportedVerbs = new List<DataVerb>() { DataVerb.GET },
					MetaAttributes = new Dictionary<string, string>()
				};
				Res1.MetaAttributes["name"] = "Example";

				List<Resource> Resources = new List<Resource>();
				Resources.Add(Res1);
				var Awaiter = CDOUplink.RegisterResources(Resources);
				Awaiter.Wait();

				Console.WriteLine("Success: {0}", Awaiter.Result);
			}

			void RunDataStorage()
			{
				SetClaimKey();
				RegisterResources();

				Quit.WaitOne();
			}

			void RunReader()
			{
				StartReadSession();
				//Quit.WaitOne();
				Quit.Set();
			}

			void RunWriter()
			{
				Quit.WaitOne();
			}

			public TestCDO(Role MyRole, string MyCertificatePath, JID MyJid)
			{
				Console.WriteLine("Loading certificate from file");
				var Cert = new System.Security.Cryptography.X509Certificates.X509Certificate2(MyCertificatePath);

				Console.WriteLine("Connecting to XMPP");


				Uplink = new Connection("sandbox.clayster.com",
				                        5222,
				                        MyJid,
				                        null,
				                        Cert,
				                        true);
				Uplink.OnConnectionStateChanged += Uplink_OnConnectionStateChanged;
				Uplink.Connect();

				this.MyRole = MyRole;

				Console.WriteLine("Waiting for uplink in role " + MyRole.ToString());
				while (CDOUplink == null)
				{
					Thread.Sleep(10);
				}



				switch (MyRole)
				{
					case Role.DataStorage:
						RunDataStorage();
						break;
					case Role.Reader:
						RunReader();
						break;
					case Role.Writer:
						RunWriter();
						break;
				}

			}

			void Uplink_OnConnectionStateChanged(Connection.CallbackConnectionState NewState)
			{
				if (NewState != Connection.CallbackConnectionState.Connected)
				{
					Console.WriteLine("Still not connected: {0}", NewState);
					return;
				}
				
				Console.WriteLine("Connecting to CDO");
				CDOUplink = new CDOController(Uplink, new JID("cdo.sandbox.clayster.com"), null, null);
			}
		}


		public static void Main(string[] args)  
		{
			try
			{
				Console.WriteLine("Starting example with CDO");

				System.Threading.Thread SourceThread = new System.Threading.Thread(() =>
					{
						try
						{
							using (TestCDO Test = new TestCDO(TestCDO.Role.DataStorage,
												  "../../testsource.clayster.pfx",
												  new JID("testsource.clayster@sandbox.clayster.com/jox")))
							{
								// No
							}
						}
						catch (System.Exception ex)
						{
							Console.WriteLine("Exception in SourceThread:");
							Console.WriteLine(ex.ToString());
						}
					} );


				System.Threading.Thread ControllerThread = new System.Threading.Thread(() =>
					{
						try
						{
							using (TestCDO Test = new TestCDO(TestCDO.Role.Reader,
												  "../../testcontroller.clayster.pfx",
												  new JID("testcontroller.clayster@sandbox.clayster.com/jox")))
							{
								// No
							}
						}
						catch (System.Exception ex)
						{
							Console.WriteLine("Exception in ControllerThread:");
							Console.WriteLine(ex.ToString());
						}
					});

				System.Threading.Thread WriterThread = new System.Threading.Thread(() =>
					{
						try
						{
							using (TestCDO Test = new TestCDO(TestCDO.Role.Writer,
												  "../../testcontrollerrandomwritertest.clayster.pfx",
												  new JID("testcontrollerrandomwritertest.clayster@sandbox.clayster.com/jox")))
							{
								// No
							}
						}
						catch (System.Exception ex)
						{
							Console.WriteLine("Exception in WriterThread:");
							Console.WriteLine(ex.ToString());
						}
					});

				SourceThread.Start();
				ControllerThread.Start();
				//WriterThread.Start();
				/*
				Console.WriteLine("Waiting for controller");
				ControllerThread.Join();
				Console.WriteLine("Waiting for writer");
				WriterThread.Join();
				Console.WriteLine("Waiting for source");
				SourceThread.Join();
				Console.WriteLine("Quiting");*/
			}
			catch(System.Exception ex)
			{
				Console.WriteLine("Exception:");
				Console.WriteLine(ex.ToString());
			}
		}
	}
}
