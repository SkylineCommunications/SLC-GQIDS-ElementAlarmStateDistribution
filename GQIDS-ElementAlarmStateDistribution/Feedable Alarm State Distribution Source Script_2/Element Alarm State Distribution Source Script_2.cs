using System;
using System.Globalization;
using Skyline.DataMiner.Analytics.GenericInterface;
using Skyline.DataMiner.Net;
using Skyline.DataMiner.Net.Messages;

[GQIMetaData(Name = "Element alarm state distribution")]
public class FeedableAlarmStateDistributionSource : IGQIDataSource, IGQIOnInit, IGQIInputArguments
{
	private GQIDMS _dms;
	private readonly GQIStringArgument _elementIDArg;
	private readonly GQIDateTimeArgument _startTimeArg;
	private readonly GQIDateTimeArgument _endTimeArg;
	private ElementID _elementID;
	private DateTime _utcStart;
	private DateTime _utcEnd;

	private readonly GQIStringColumn _stateNameColumn;
	private readonly GQIDoubleColumn _stateProportionColumn;

	public FeedableAlarmStateDistributionSource()
	{
		_elementIDArg = new GQIStringArgument("Element ID");
		_startTimeArg = new GQIDateTimeArgument("Start time");
		_endTimeArg = new GQIDateTimeArgument("End time");

		_stateNameColumn = new GQIStringColumn("State");
		_stateProportionColumn = new GQIDoubleColumn("Proportion");
	}

	public OnInitOutputArgs OnInit(OnInitInputArgs args)
	{
		_dms = args.DMS;
		return default;
	}

	public GQIArgument[] GetInputArguments()
	{
		return new GQIArgument[]
		{
			_elementIDArg,
			_startTimeArg,
			_endTimeArg
		};
	}

	public OnArgumentsProcessedOutputArgs OnArgumentsProcessed(OnArgumentsProcessedInputArgs args)
	{
		_elementID = ResolveElementID(args);
		_utcStart = args.GetArgumentValue(_startTimeArg);
		_utcEnd = args.GetArgumentValue(_endTimeArg);
		return default;
	}

	public GQIColumn[] GetColumns()
	{
		return new GQIColumn[]
		{
			_stateNameColumn,
			_stateProportionColumn
		};
	}

	public GQIPage GetNextPage(GetNextPageInputArgs args)
	{
		var states = GetStates() ?? GetDefaultStates();
		var rows = new[]
		{
			CreateStateRow("Critical", states.PercentageCritical),
			CreateStateRow("Major", states.PercentageMajor),
			CreateStateRow("Masked", states.PercentageMasked),
			CreateStateRow("Minor", states.PercentageMinor),
			CreateStateRow("Normal", states.PercentageNormal),
			CreateStateRow("No template", states.PercentageNoTemplate),
			CreateStateRow("Timeout", states.PercentageTimeout),
			CreateStateRow("Unknown", states.PercentageUnknown),
			CreateStateRow("Warning", states.PercentageWarning),
		};
		return new GQIPage(rows) { HasNextPage = false };
	}

	private GQIRow CreateStateRow(string name, double percentage)
	{
		var proportion = percentage / 100;
		var nameCell = new GQICell { Value = name };
		var proportionCell = new GQICell
		{
			Value = proportion,
			DisplayValue = proportion.ToString("P")
		};
		return new GQIRow(new[] { nameCell, proportionCell });
	}

	private ElementID ResolveElementID(OnArgumentsProcessedInputArgs args)
	{
		var elementID = args.GetArgumentValue(_elementIDArg);
		return ElementID.FromString(elementID);
	}

	private ReportStateDataResponseMessage GetStates()
	{
		var request = CreateStateRequest();
		if (request is null)
			return default;

		var response = _dms.SendMessage(request);
		return response as ReportStateDataResponseMessage;
	}

	private ReportStateDataResponseMessage GetDefaultStates()
	{
		return new ReportStateDataResponseMessage();
	}

	private GetReportStateDataMessage CreateStateRequest()
	{
		if (_elementID is null)
			return default;

		var filter = ReportFilterInfo.Element(_elementID.DataMinerID, _elementID.EID);
		var timeRange = GetDMATimeRangeString(_utcStart, _utcEnd);
		return new GetReportStateDataMessage
		{
			MaxAmount = 0,
			Timespan = timeRange,
			Filter = filter
		};
	}

	private string GetDMADateTimeString(DateTime utcTime)
	{
		var localTime = utcTime.ToLocalTime();
		return localTime.ToString("yyyy'-'MM'-'dd HH':'mm':'ss", CultureInfo.InvariantCulture);
	}

	private string GetDMATimeRangeString(DateTime utcStart, DateTime utcEnd)
	{
		var startTime = GetDMADateTimeString(utcStart);
		var endTime = GetDMADateTimeString(utcEnd);
		return $"{startTime}|{endTime}";
	}
}