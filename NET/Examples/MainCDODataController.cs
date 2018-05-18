using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;

using CDOLWTSD;

using DXMPP;

namespace ClaysterSamples
{
	public class MainCDODataController
	{
		class ExampleController : IDisposable
		{
			Connection Uplink;
			volatile CDODataController DataController;
			JID Orchestrator = new JID("cdo.sandbox.clayster.com");

			Roster.SubscribeResponse Roster_OnSubscribe(JID From)
			{
				return Roster.SubscribeResponse.AllowAndSubscribe;
			}

			void Roster_OnUnsubscribed(JID From)
			{
				Uplink.Roster.Unsubscribe(From);
			}

			void DumpDataToConsole(LWTSD.DataController.DataPage Page)
			{
				foreach (var Resource in Page.Data)
				{
					LWTSD.ResourceTypes.ResourceInteger RInt = Resource as LWTSD.ResourceTypes.ResourceInteger;

					if (RInt != null)
					{
						Console.WriteLine("{0} = {1}", Resource.Path, RInt.Value);
						continue;
					}

					LWTSD.ResourceTypes.ResourceString RString = Resource as LWTSD.ResourceTypes.ResourceString;
					if (RString != null)
					{
						Console.WriteLine("{0} = {1}", Resource.Path, RString.Value);
						continue;
					}

					LWTSD.ResourceTypes.ResourceBoolean RBoolean = Resource as LWTSD.ResourceTypes.ResourceBoolean;
					if (RBoolean != null)
					{
						Console.WriteLine("{0} = {1}", Resource.Path, RBoolean.Value);
						continue;
					}

					LWTSD.ResourceTypes.ResourceDouble RDouble = Resource as LWTSD.ResourceTypes.ResourceDouble;
					if (RDouble != null)
					{
						Console.WriteLine("{0} = {1}", Resource.Path, RDouble.Value);
						continue;
					}


				}
			}


			public ExampleController()
			{
                string CertPath = "../../../YOURCERTHERE.pfx";
                var Cert = new System.Security.Cryptography.X509Certificates.X509Certificate2(CertPath);

                Uplink = new Connection("164.138.24.100",
                            5222,
                            new JID("", "sandbox.clayster.com", ""), 
                            "", 
                            Cert, 
                            true, 30,0);

				Uplink.Roster.OnSubscribe = Roster_OnSubscribe;
				Uplink.Roster.OnUnsubscribed = Roster_OnUnsubscribed;

				Uplink.OnConnectionStateChanged += Uplink_OnConnectionStateChanged;
				Uplink.Connect();

				while (DataController == null)
					Thread.Sleep(10);


                DataController.RequestedResources["meters/cpuusage"] = new CDO.ResourceAccess()
				{
                    ResourcePath = "meters/cpuusage",
					Verbs = new List<CDO.DataVerb>() { CDO.DataVerb.GET }
				};
				var awaiter = DataController.ReadData();
				awaiter.Wait();
				if (awaiter.Result == null)
				{
					Console.WriteLine("Read data failed");
				}

				DumpDataToConsole(awaiter.Result);
			}


			void Uplink_OnConnectionStateChanged(Connection.CallbackConnectionState NewState)
			{
				if (NewState == Connection.CallbackConnectionState.Connected
				  && DataController == null)
                    DataController = new CDODataController(Uplink, Orchestrator, "ENTITYIDHERE");
				
			}

			public void Dispose()
			{
				DataController.Dispose();
				Uplink.Dispose();
			}
		}

		public static void Main(string[] args)
		{

			try
			{
				Console.WriteLine("Starting MainCDODataController");
				using (var jox = new ExampleController())
				{
					while (true)
					{
						Thread.Sleep(100);
					}
				}
				Console.WriteLine("Quiting MainCDODataController");
			}
			catch (System.Exception ex)
			{
				Console.WriteLine(ex.ToString());
			}
			catch
			{
				Console.WriteLine("Unknown exception");
			}
		}
	}
}
