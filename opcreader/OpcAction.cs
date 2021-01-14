
using System;

namespace OpcReader
{
	using CsvHelper;
	using Opc.Ua;
	using static Program;

	/// <summary>
	/// Class to manage OPC sessions.
	/// </summary>
	public class OpcAction
	{
		/// <summary>
		/// Next action id.
		/// </summary>
		private static uint IdCount = 0;

		/// <summary>
		/// Instance action id.
		/// </summary>
		public uint Id;

		/// <summary>
		/// Endpoint URL of the target server.
		/// </summary>
		public string EndpointUrl;

		/// <summary>
		/// Configured id of action target node.
		/// </summary>
		public string OpcNodeId;

		/// <summary>
		/// Recurring interval of action in sec.
		/// </summary>
		public int Interval;

		/// <summary>
		/// Next execution of action in utc ticks.
		/// </summary>
		public long NextExecution;

		/// <summary>
		/// OPC UA node id of action target node.
		/// </summary>
		public NodeId OpcUaNodeId;

		/// <summary>
		/// Description of action.
		/// </summary>
		public string Description => $"ActionId: {Id:D3} ActionType: '{GetType().Name}', Endpoint: '{EndpointUrl}' Node '{OpcNodeId}'";

		/// <summary>
		/// Ctor for the action.
		/// </summary>
		public OpcAction(Uri endpointUrl, string opcNodeId, int interval)
		{
			Id = IdCount++; ;
			EndpointUrl = endpointUrl.AbsoluteUri;
			Interval = interval;
			OpcNodeId = opcNodeId;
			NextExecution = DateTime.UtcNow.Ticks;
			OpcUaNodeId = null;
		}

		/// <summary>
		/// Generate receiver's opcUaNodeId from its opcNodeId.
		/// </summary>
		protected virtual void NormalizeNodeId(OpcSession session)
		{
				OpcUaNodeId = session.GetNodeIdFromId(OpcNodeId);
				Logger.Debug($"NodeId to query: «{OpcUaNodeId}».");
		}

		/// <summary>
		/// Execute function needs to be overloaded.
		/// </summary>
		public virtual void Execute(OpcSession session)
		{
			Logger.Error($"No Execute method for action ({Description}) defined.");
			throw new Exception($"No Execute method for action ({ Description}) defined.");
		}

		/// <summary>
		/// Report result of action.
		/// </summary>
		public virtual void ReportResult(ServiceResultException sre)
		{
			if (sre == null)
			{
				ReportSuccess();
			}
			else
			{
				ReportFailure(sre);
			}
		}

		/// <summary>
		/// Report successful action execution.
		/// </summary>
		public virtual void ReportSuccess()
		{
			Logger.Information($"Action ({Description}) completed successfully");
		}

		/// <summary>
		/// Report failed action execution.
		/// </summary>
		public virtual void ReportFailure(ServiceResultException sre)
		{
			Logger.Information($"Action ({Description}) execution with error");
			if (sre == null)
			{
				Logger.Information($"Result ({Description}): Unknown error");
			}
			else {
				Logger.Information($"Result ({Description}): {sre.Result}");
				if (sre.InnerException != null)
				{
					Logger.Information($"Details ({Description}): {sre.InnerException.Message}");
				}
			}
		}

		protected virtual void LogExecutionStart(OpcSession session)
		{
			Logger.Information($"Start action {Description} on '{session.EndpointUrl}'");
			Logger.Debug($"NodeId to query: «{OpcUaNodeId}».");
		}
	}

	/// <summary>
	/// Class to describe a read action.
	/// </summary>
	public class OpcReadAction : OpcAction
	{
		/// <summary>
		/// Value read by the action.
		/// </summary>
		protected DataValue dataValue1;

		/// <summary>
		/// Ctor for the read action.
		/// </summary>
		public OpcReadAction(Uri endpointUrl, ReadActionModel action) : base(endpointUrl, action.Id, action.Interval)
		{
		}
		public OpcReadAction(Uri endpointUrl, String id) : base(endpointUrl, id, 0)
		{
		}

		/// <inheritdoc />
		public override void Execute(OpcSession session)
		{
			LogExecutionStart(session);
			NormalizeNodeId(session);
			// read the node info
			Node node = session.OpcUaClientSession.ReadNode(OpcUaNodeId);

			// report the node info
			Logger.Information($"Node Displayname is '{node.DisplayName}'");
			Logger.Information($"Node Description is '{node.Description}'");

			Logger.Debug($"Node id: «{node.NodeId}»; browse name: «{node.BrowseName}»; xml encoding id: «{node.XmlEncodingId}».");

			// read the value
			dataValue1 = session.OpcUaClientSession.ReadValue(node.NodeId);
			
			// report the node value
			Logger.Information($"Node Value is '{dataValue1.Value}'");
			Logger.Information($"Node Value is '{dataValue1}'");

			if(Program.HaveToWriteCsv)
				WriteToCsvFile();
		}

		/// <inheritdoc />
		public override void ReportResult(ServiceResultException sre)
		{
			if (dataValue1 is null)
				base.ReportFailure(sre);
			else
				base.ReportResult(sre);
		}

		/// <inheritdoc />
		public override void ReportSuccess()
		{
			Logger.Information($"Action ({Description}) completed successfully");
			Logger.Information($"Value ({Description}): {dataValue1.Value}");
		}

        /// <summary>
        /// Write the receiver to the CSV file.
        /// </summary>
        protected virtual void WriteToCsvFile()
		{
			TMValue tmvalue =
				new TMValue(
						OpcNodeId.ToString(), 
						dataValue1.ServerTimestamp, 
						dataValue1.WrappedValue.ToString(),
						dataValue1.StatusCode.ToString()
					);
			tmvalue.WriteToCsvFile();
		}

	}

	/// <summary>
	/// Class to describe the action of reading the last value at a time.
	/// </summary>
	public class OpcHistoryReadAction : OpcReadAction
	{
		private ReadRawModifiedDetails details;

		public OpcHistoryReadAction(Uri endpointUrl, HistoryReadActionModel hrActionModel) : base(endpointUrl, hrActionModel.Id)
		{
			details = new ReadRawModifiedDetails
			{
				StartTime = hrActionModel.StartTime,
				EndTime = hrActionModel.EndTime,
				IsReadModified = hrActionModel.IsReadModified,
				NumValuesPerNode = hrActionModel.NumValuesPerNode,
				ReturnBounds = hrActionModel.ReturnBounds
			};
		}

		public override void Execute(OpcSession session)
		{
			LogExecutionStart(session);
			NormalizeNodeId(session);
			HistoryReadValueIdCollection nodesToRead = new HistoryReadValueIdCollection
			{
				new HistoryReadValueId{ NodeId = OpcUaNodeId }
			};

			Opc.Ua.Client.Session uaSession = session.OpcUaClientSession;
			Logger.Debug("About to perform a HistoryRead:");
			Logger.Debug("Details:");
			Logger.Debug($"  Start time:  {details.StartTime}");
			Logger.Debug($"  End time:  {details.EndTime}");
			Logger.Debug($"  Read modified:  {details.IsReadModified}");
			Logger.Debug($"  Values per node:  {details.NumValuesPerNode}");
			Logger.Debug($"  Return bounds:  {details.ReturnBounds}");
			Logger.Debug("Nodes to read:");
			nodesToRead.ForEach(n => Logger.Debug($"  {n}({n.NodeId})"));
			uaSession.HistoryRead(
				null,
				new ExtensionObject(details),
				TimestampsToReturn.Both,
				false,
				nodesToRead,
				out HistoryReadResultCollection results,
				out DiagnosticInfoCollection diagnostics
			);
			Logger.Debug($"HistoryRead got {results.Count} results, {diagnostics.Count} diagnostics.");
			HistoryReadResult hrr = results[0];
			HistoryData histData = (HistoryData)ExtensionObject.ToEncodeable(hrr.HistoryData);
			if (StatusCode.IsBad(hrr.StatusCode))
				Logger.Information($"Bad result ({hrr.StatusCode}) reading {OpcNodeId}");
			else {
				if (StatusCode.IsGood(hrr.StatusCode))
					Logger.Debug($"Good result: {histData}, {histData.DataValues.Count} values.");			
				if (StatusCode.IsUncertain(hrr.StatusCode))
					Logger.Information($"Uncertain result: {hrr}");
			}
			foreach (DataValue dv in histData.DataValues)
			{
				Logger.Debug($"  {dv} ({dv.SourceTimestamp})");
				dataValue1 = new DataValue(); // Acá debería asignarse algo que viene desde «results».
				if (Program.HaveToWriteCsv)
					WriteDataValueToCsvFile(dv);
			}
		}

		/// <summary>
		/// Write the DataValue 'dv' to the CSV file.
		/// </summary>
		protected virtual void WriteDataValueToCsvFile(DataValue dv)
        {
			TMValue tmvalue =
				new TMValue(
						OpcNodeId.ToString(),
						dv.ServerTimestamp,
						dv.WrappedValue.ToString(),
						dv.StatusCode.ToString()
					);
			tmvalue.WriteToCsvFile();
		}
	}

	public class TMValue
	{
        public string Tag { get; set; }
        public DateTime DateAndTime { get; set; }
        public string Value { get; set; }
        public string Status { get; set; } = "";

        public TMValue(string tag, DateTime dateAndTime, string value)
		{
			Tag = tag;
			DateAndTime = dateAndTime;
			Value = value;
		}
		public TMValue(string tag, DateTime dateAndTime, string value, string status):this(tag, dateAndTime, value)
        {
			Status = status;
        }

		/// <summary>
		/// Write the receiver to the CSV file.
		/// </summary>
		public virtual void WriteToCsvFile()
		{
			CsvWriter csv = Program.CsvWriter();
			csv.WriteRecord(this);
			csv.NextRecord();
			csv.Flush();
		}
	}

	/// <summary>
	/// Class to describe a test action.
	/// </summary>
	public class OpcTestAction : OpcAction
	{
		/// <summary>
		/// Value read by the action.
		/// </summary>
		public dynamic Value;

		/// <summary>
		/// Ctor for the test action.
		/// </summary>
		public OpcTestAction(Uri endpointUrl, TestActionModel action) : base(endpointUrl, action.Id, action.Interval)
		{
		}

		/// <inheritdoc />
		public override void Execute(OpcSession session)
		{
			LogExecutionStart(session);
			NormalizeNodeId(session);
			// read the node info
			Node node = session.OpcUaClientSession.ReadNode(OpcUaNodeId);

			// report the node info
			Logger.Debug($"Action ({Description}) Node DisplayName is '{node.DisplayName}'");
			Logger.Debug($"Action ({Description}) Node Description is '{node.Description}'");

			// read the value
			DataValue dataValue = session.OpcUaClientSession.ReadValue(OpcUaNodeId);
			try
			{
				Value = dataValue.Value;
			}
			catch (Exception e)
			{
				Logger.Warning(e, $"Cannot convert type of read value.");
				Value = "Cannot convert type of read value.";
			}

			// report the node value
			Logger.Debug($"Action ({Description}) Node data value is '{dataValue.Value}'");
		}

		/// <inheritdoc />
		public override void ReportSuccess()
		{
			Logger.Information($"Action ({Description}) completed successfully");
			Logger.Information($"Value ({Description}): {Value}");
		}
	}
}
