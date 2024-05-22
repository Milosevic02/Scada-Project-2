using Common;
using System;
using System.Collections.Generic;
using System.Threading;

namespace ProcessingModule
{
    /// <summary>
    /// Class containing logic for automated work.
    /// </summary>
    public class AutomationManager : IAutomationManager, IDisposable
	{
		private Thread automationWorker;
        private AutoResetEvent automationTrigger;
        private IStorage storage;
		private IProcessingManager processingManager;
		private int delayBetweenCommands;
        private IConfiguration configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="AutomationManager"/> class.
        /// </summary>
        /// <param name="storage">The storage.</param>
        /// <param name="processingManager">The processing manager.</param>
        /// <param name="automationTrigger">The automation trigger.</param>
        /// <param name="configuration">The configuration.</param>
        public AutomationManager(IStorage storage, IProcessingManager processingManager, AutoResetEvent automationTrigger, IConfiguration configuration)
		{
			this.storage = storage;
			this.processingManager = processingManager;
            this.configuration = configuration;
            this.automationTrigger = automationTrigger;
        }

        /// <summary>
        /// Initializes and starts the threads.
        /// </summary>
		private void InitializeAndStartThreads()
		{
			InitializeAutomationWorkerThread();
			StartAutomationWorkerThread();
		}

        /// <summary>
        /// Initializes the automation worker thread.
        /// </summary>
		private void InitializeAutomationWorkerThread()
		{
			automationWorker = new Thread(AutomationWorker_DoWork);
			automationWorker.Name = "Aumation Thread";
		}

        /// <summary>
        /// Starts the automation worker thread.
        /// </summary>
		private void StartAutomationWorkerThread()
		{
			automationWorker.Start();
		}


		private void AutomationWorker_DoWork()
		{
			EGUConverter conv = new EGUConverter();
			while(!disposedValue)
			{
				List<PointIdentifier> pointList = new List<PointIdentifier>();

                //Udaljenost kapije MAX-700(Ne pise koliko je max ja stavio) na [0] mestu u listi
                pointList.Add(new PointIdentifier(PointType.ANALOG_OUTPUT, 1000));

                //Indikacija prepreke na [1] mestu u listi
                pointList.Add(new PointIdentifier(PointType.DIGITAL_INPUT, 2000));

                //Open na [2] mestu u listi
                //Ako je ukljucen oduzimamo -10 na vrednost(Udaljenost kapije) svake sekunde Otvaramo Kapiju
                pointList.Add(new PointIdentifier(PointType.DIGITAL_OUTPUT, 3000));

                //Close na [3] mestu u listi
                //Ako je ukljucen dodajemo +10 na vrednost(Udaljenost kapije) svake sekunde Zatvaramo Kapiju
                pointList.Add(new PointIdentifier(PointType.DIGITAL_OUTPUT, 3001));

				List<IPoint> points = storage.GetPoints(pointList);
				ushort value = points[0].RawValue;

				//Ako je ukljuceno otvaranje
				if (points[2].RawValue == 1)
				{
					//Otvaraj kapiju smanjuj vrednost za 10cm svake sekunde
					value -= conv.ConvertToRaw(points[0].ConfigItem.ScaleFactor, points[0].ConfigItem.Deviation, 10);

					//Ako je kapija dosla do LowLimita = 20cm onda treba da se ugasi otvaranje
					if(value < points[0].ConfigItem.LowLimit)
					{
						//Ugasimo Otvaranje,Prikazujemo na simulatoru
						processingManager.ExecuteWriteCommand(points[2].ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, 3000, 0);
					}
					else
					{
						//Prikazujemo na simulatoru oduzetu vrednost
						processingManager.ExecuteWriteCommand(points[0].ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, 1000, value);
					}
				}

				if (points[3].RawValue == 1)
				{
					//Ako postoji prepreka
					if (points[1].RawValue == 1)
					{
						//Otvori kapiju do LowAlarma
						value -= conv.ConvertToRaw(points[0].ConfigItem.ScaleFactor, points[0].ConfigItem.Deviation, 10);

					}
					else
					{
						//Zatvaramo kapiju povecavamo vrednost za 10cm svake sekunde
						value += conv.ConvertToRaw(points[0].ConfigItem.ScaleFactor, points[0].ConfigItem.Deviation, 10);
					}

					//Ako je kapija dosla do HighLimita = 600cm onda treba da se ugasi zatvaranje || Ako je kapija dosla do LowLimit = 20cm onda treba da se ugasi zatvaranje
					if(value > points[0].ConfigItem.HighLimit || value < points[0].ConfigItem.LowLimit)
					{
						//Ugasimo Zatvaranje,Prikazujemo na simulatoru 
						processingManager.ExecuteWriteCommand(points[3].ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, 3001, 0);

					}
					else
					{
						//Prikazujemo na simulatoru sabranu vrednost
						processingManager.ExecuteWriteCommand(points[0].ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, 1000, value);
					}
				}
				automationTrigger.WaitOne(delayBetweenCommands);



            }
		}

		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls


        /// <summary>
        /// Disposes the object.
        /// </summary>
        /// <param name="disposing">Indication if managed objects should be disposed.</param>
		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
				}
				disposedValue = true;
			}
		}


		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
			// GC.SuppressFinalize(this);
		}

        /// <inheritdoc />
        public void Start(int delayBetweenCommands)
		{
			this.delayBetweenCommands = delayBetweenCommands*1000;
            InitializeAndStartThreads();
		}

        /// <inheritdoc />
        public void Stop()
		{
			Dispose();
		}
		#endregion
	}
}
