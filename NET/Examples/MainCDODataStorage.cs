using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;

using CDOLWTSD;

using DXMPP;

namespace ClaysterSamples
{
	public class MainCDODataStorage
	{
		class ExampleStorageEntity : IDisposable
		{
			Connection Uplink;
			volatile CDODataStorageEntity DataStorage;
			JID Orchestrator = new JID("cdo.sandbox.clayster.com");
			PerformanceCounter CPUCounter = new PerformanceCounter ("Processor", "% Processor Time", "_Total");
			PerformanceCounter MemoryMBAvailableCounter = new PerformanceCounter("Memory", "Available MBytes");
			LWTSD.ResourceTypes.ResourceInteger CPUUsage;
			LWTSD.ResourceTypes.ResourceInteger AvailableMemory;
			LWTSD.ResourceTypes.ResourceString ScratchPad;

			System.Timers.Timer UpdateResourcesTimer;

			void UpdateResources()
			{
				int OldCPUUsage = CPUUsage.Value;
				int OldAvailableMemory = AvailableMemory.Value;

				CPUUsage.Value = (int) (CPUCounter.NextValue() + 0.5);
				AvailableMemory.Value = (int) (MemoryMBAvailableCounter.NextValue() + 0.5);

				if (CPUUsage.Value != OldCPUUsage 
				    || OldAvailableMemory != AvailableMemory.Value)
				{
					DataStorage.InvalidateSubscriptions();
				}
			}

			Roster.SubscribeResponse Roster_OnSubscribe(JID From)
			{
				return Roster.SubscribeResponse.AllowAndSubscribe;
			}

			void Roster_OnUnsubscribed(JID From)
			{
				Uplink.Roster.Unsubscribe(From);
			}

			public ExampleStorageEntity()
			{
				string CertPath = "../../testsource.clayster.pfx";
                var Cert = new System.Security.Cryptography.X509Certificates.X509Certificate2(CertPath);

                Uplink = new Connection("164.138.24.100",
							5222,
                            "sandbox.clayster.com",
							Cert,
							0 /* Debug level */);

				Uplink.Roster.OnSubscribe = Roster_OnSubscribe;
				Uplink.Roster.OnUnsubscribed = Roster_OnUnsubscribed;

				Uplink.OnConnectionStateChanged += Uplink_OnConnectionStateChanged;
				Uplink.Connect();

				while (DataStorage == null)
					Thread.Sleep(10);

                // Set claim key
                string MyClaimKey = Cert.GetCertHashString();
                var awaiter = DataStorage.SessionController.SetClaimKey(MyClaimKey);
                awaiter.Wait();
                Console.WriteLine("Set claim key to {0} = ", awaiter.Result);

				CPUUsage = new LWTSD.ResourceTypes.ResourceInteger();
				CPUUsage.Path = "meters/cpuusage";
				CPUUsage.Displayname = "CPU%";
				CPUUsage.SupportsRead = true;
				CPUUsage.SupportsWrite = false;
				CPUUsage.Unit = "percentage";
				CPUUsage.Value = 0;

				AvailableMemory = new LWTSD.ResourceTypes.ResourceInteger();
				AvailableMemory.Path = "meters/availablememory";
				AvailableMemory.Displayname = "Available Memory";
				AvailableMemory.SupportsRead = true;
				AvailableMemory.SupportsWrite = false;
				AvailableMemory.Unit = "MB";
				AvailableMemory.Value = 0;

				ScratchPad = new LWTSD.ResourceTypes.ResourceString();
				ScratchPad.Path = "misc/scratchpad";
				ScratchPad.Displayname = "Scratchad";
				ScratchPad.SupportsRead = true;
				ScratchPad.SupportsWrite = true;
				ScratchPad.Value = "Nothing";

				DataStorage.AddResource(CPUUsage);
				DataStorage.AddResource(AvailableMemory);
				DataStorage.AddResource(ScratchPad);
				DataStorage.RegisterResources().Wait();

				UpdateResourcesTimer = new System.Timers.Timer(1000.0);
				UpdateResourcesTimer.AutoReset = true;
				UpdateResourcesTimer.Elapsed += (sender, e) => { UpdateResources(); };
				UpdateResourcesTimer.Start();
			}


			void Uplink_OnConnectionStateChanged(Connection.CallbackConnectionState NewState)
			{
				if (NewState == Connection.CallbackConnectionState.Connected
				  && DataStorage == null)
					DataStorage = new CDODataStorageEntity(Uplink, Orchestrator);
			}

			public void Dispose()
			{
				UpdateResourcesTimer.Dispose();
				DataStorage.Dispose();
				Uplink.Dispose();
			}
		}
		
		public static void Main(string[] args)
		{			
			
			Console.WriteLine("Starting MainCDOHighlevel");
			using (var jox = new ExampleStorageEntity())
			{
				while (true)
				{
					Thread.Sleep(100);
				}
			}
			Console.WriteLine("Quiting MainCDOHighlevel");
		}
	}
}
