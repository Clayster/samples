using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using DXMPP;

namespace ClaysterSamples
{
    class MainClass
    {
		// Todo: Fix wait handles here.. Getting messy.
		static volatile bool SourceStarted = false;
		static volatile bool ControllerStarted = false;
		static volatile bool RequestWrite = false;
		static volatile bool WriterHasWritten = false;
		static volatile bool Quit = false;
		static volatile bool GotSubscriptionData = false;

        class SourceTest : IDisposable
        {
            Connection Connection;
            LWTSD.DataSource DataHandler;

            public SourceTest()
            {
                Console.WriteLine("Connecting.");

				Connection = new Connection("baldershage-stefan.clayster.com",
                                            5222,
                                            new JID("testsource@baldershage-stefan.clayster.com/sourcetest"),
                                            "test1234",
				                           null,
				                           true);
				Connection.OnConnectionStateChanged += OnConnectionStateChanged;
                Connection.Connect();

                while (!Quit)
                {
                    System.Threading.Thread.Sleep(100);
                }
			}

			public void Dispose()
			{
				Connection.Dispose();
			}

			public void OnConnectionStateChanged(Connection.CallbackConnectionState State)
            {
				if (State != Connection.CallbackConnectionState.Connected)
				{
					Console.WriteLine("Still waiting for being connected, new state: " + State.ToString());
					return;
				}
				
                Console.WriteLine("Connected. Starting data source test.");
                DataHandler = new LWTSD.DataSource(Connection, false);

				// Some thermometers
				for (int i = 0; i < 3; i++)
                {
                    LWTSD.ResourceTypes.ResourceInteger Point = new LWTSD.ResourceTypes.ResourceInteger();
                    Point.Path = "thermometers/temp" + i.ToString();
                    Point.Displayname = "Temperature " + i.ToString();
                    Point.Description = "A temperature";
                    Point.Unit = "Celcius";
                    Point.SupportsRead = true;
                    Point.SupportsWrite = false;
                    Point.Value = 20 + i;
                    Point.MinExclusive = 3;
                    DataHandler.AddResource(Point);
                }

				// Some writables
				for (int i = 0; i < 3; i++)
				{
					LWTSD.ResourceTypes.ResourceInteger Point = new LWTSD.ResourceTypes.ResourceInteger();
					Point.Path = "writables/item" + i.ToString();
					Point.Displayname = "Writable " + i.ToString();
					Point.Description = "A writable thingy";
					Point.Unit = "Unknown";
					Point.SupportsRead = true;
					Point.SupportsWrite = true;
					Point.Value = i+1;
					Point.MinExclusive = 0;
                    Point.MaxExclusive = 10;
					DataHandler.AddResource(Point);
				}

				SourceStarted = true;
            }

		}

		class ControllerTest : IDisposable
		{
			Connection Connection;
			LWTSD.DataController Client;

			public void Dispose()
			{
				Connection.Dispose();
			}

			void DumpDataToConsole(LWTSD.DataController.DataPage Page)
			{
				foreach (var Resource in Page.Data)
				{
					LWTSD.ResourceTypes.ResourceInteger RInt = Resource as LWTSD.ResourceTypes.ResourceInteger;

					if (RInt != null)
					{
						Console.WriteLine("{0} = {1}", Resource.Path,
										  RInt.Value);
						continue;
					}

					LWTSD.ResourceTypes.ResourceString RString = Resource as LWTSD.ResourceTypes.ResourceString;
					if (RString != null)
					{
						Console.WriteLine(Resource.Path + " = " + RString.Value);
						continue;
					}
				}
			}

			public ControllerTest()
			{
				Console.WriteLine("Connecting.");

				Connection = new Connection("baldershage-stefan.clayster.com",
											5222,
											new JID("testcontroller@baldershage-stefan.clayster.com/controllertest"),
											"test1234",
										   null,
										   true);
				Connection.OnConnectionStateChanged += OnConnectionStateChanged;
				Connection.Connect();

				while (Client == null && !Quit)
				{
					System.Threading.Thread.Sleep(100);
				}
				Console.WriteLine("Requesting schema");
				var SchemaWaiter = Client.GetSchema(0, "dummysession");
				Console.WriteLine("Waiting for result");
				SchemaWaiter.Wait();
				Console.WriteLine("Got Schema");
				LWTSD.SimplifiedSchema Schema = SchemaWaiter.Result;
				//Console.WriteLine(Schema.GetSerializedElement(0, 1000).ToString());

				Console.WriteLine("Requesting data");
				List<LWTSD.ResourcePath> ReadResources = new List<LWTSD.ResourcePath>();
				foreach (var res in Schema.Resources.Values)
				{
					if (!res.SupportsRead)
						continue;

					Console.WriteLine("adding resource to get: " + res.Path);
					ReadResources.Add(res.Path);
				}

				var DataWaiter = Client.ReadData(Schema, ReadResources, "dummysession", 0);
				Console.WriteLine("Waiting for result");
				DataWaiter.Wait();
				Console.WriteLine("Got data.");
				LWTSD.DataController.DataPage Page = DataWaiter.Result;
				if (Page.Data == null)
				{
					throw new Exception("Data is null");
				}
				DumpDataToConsole(Page);

				Console.WriteLine("Subscriping to data");
				var SubscriptionAwaiter = Client.SubscribeToData(Schema, ReadResources,
																 (Tuple<string, LWTSD.DataController.DataPage> obj) =>
																{

																	Console.WriteLine("Got new data on subscription " + obj.Item1);
																	DumpDataToConsole(obj.Item2);
																	GotSubscriptionData = true;
																},
																 (string obj) =>
																{

																	Console.WriteLine("Subscription cancelled with id {0}", obj);
																}				                                                 
				                                                 ,
				                                                 "dummysession");

				Console.WriteLine("Waiting for subscription");
				SubscriptionAwaiter.Wait();
				Console.WriteLine("Got subscription: " + SubscriptionAwaiter.Result);

				RequestWrite = true;
				while (!GotSubscriptionData && !Quit)
				{
					Console.WriteLine("Waiting for subscriptiondata");
					Thread.Sleep(100);
				}
			}

			public void OnConnectionStateChanged(Connection.CallbackConnectionState State)
			{
				if (State != Connection.CallbackConnectionState.Connected)
				{
					Console.WriteLine("Still waiting for being connected, new state: " + State.ToString());
				   	return;
				}

				while (!SourceStarted && !Quit)
				{
					Console.WriteLine("Waiting for source to be started");
					Thread.Sleep(100);
				}
				Console.WriteLine("Connected. Starting controller test");
				Client = new LWTSD.DataController(Connection, 
				                                  new JID("testsource@baldershage-stefan.clayster.com/sourcetest"));
				
			}

		}

		class RandomWriterTest : IDisposable
		{
			Connection Connection;
			LWTSD.DataController Client;

			public void Dispose()
			{
				Connection.Dispose();
			}

			public RandomWriterTest()
			{
				Console.WriteLine("Connecting.");

				Connection = new Connection("baldershage-stefan.clayster.com",
											5222,
											new JID("testcontrollerrandomwritertest@baldershage-stefan.clayster.com/controllertest"),
											"test1234",
										   null,
										   true);
				Connection.OnConnectionStateChanged += OnConnectionStateChanged;
				Connection.Connect();

				while (Client == null)
				{
					System.Threading.Thread.Sleep(100);
				}

				LWTSD.DataController.DataPage Page = new LWTSD.DataController.DataPage();
				LWTSD.ResourceTypes.ResourceInteger Writable1 = 
					new LWTSD.ResourceTypes.ResourceInteger();
				Writable1.Path = "writables/item1";
				Writable1.Value = 3;
				Page.Data.Add(Writable1);
				Console.WriteLine("Setting {0} to {1}", Writable1.Path, Writable1.Value);
				Console.WriteLine("Flushing data");
				var WriteDataAwaiter = Client.WriteData(Page, "dummysession");
				Console.WriteLine("Data flushed. Waiting for ack.");
				WriteDataAwaiter.Wait();
				Console.WriteLine("Data written. Success: " + WriteDataAwaiter.Result.ToString());
				if (WriteDataAwaiter.Result == false)
				{
					Console.WriteLine("Quiting because of error");
					Quit = true;
					return;
				}

				WriterHasWritten = true;

				while (!Quit)
				{
					Thread.Sleep(100);
				}
			}

			public void OnConnectionStateChanged(Connection.CallbackConnectionState State)
			{
				if (State != Connection.CallbackConnectionState.Connected)
				{
					Console.WriteLine("Still waiting for being connected, new state: " + State.ToString());
					return;
				}

				while (!RequestWrite)
				{
					Thread.Sleep(100);
				}
				Console.WriteLine("Writer is writing");
				Client = new LWTSD.DataController(Connection,
												  new JID("testsource@baldershage-stefan.clayster.com/sourcetest"));


				Console.WriteLine("Writer has written");
			}

		}
        
        public static void Main(string[] args)
        {
			System.Threading.Thread SourceThread = new System.Threading.Thread(() => 
				{
					try
					{
						using (new SourceTest()) ;
					}
					catch(System.Exception ex)
					{
						Console.WriteLine("Exception in SourceThread:");
						Console.WriteLine(ex.ToString());
					}
				} 
			);
			System.Threading.Thread ControllerThread = new System.Threading.Thread(() => 
				{ 
					try
					{
						using (new ControllerTest()) ;
					}
					catch (System.Exception ex)
					{
						Console.WriteLine("Exception in ControllerThread:");
						Console.WriteLine(ex.ToString());
					}
				} 
          	);

			System.Threading.Thread ControllerThreadWriter = new System.Threading.Thread(() =>
				{
					try
					{
						using (new RandomWriterTest()) ;
					}
					catch (System.Exception ex)
					{
						Console.WriteLine("Exception in ControllerThreadWriter:");
						Console.WriteLine(ex.ToString());
					}
				}
			  );

			SourceThread.Start();
			ControllerThread.Start();
			ControllerThreadWriter.Start();

			Console.WriteLine("Waiting for controller");
			ControllerThread.Join();
			Console.WriteLine("Quiting");
			Quit = true;
			Console.WriteLine("Waiting for writer");
			ControllerThreadWriter.Join();
			Console.WriteLine("Waiting for source");
			SourceThread.Join();
			Console.WriteLine("Quiting");
        }

    }
}
