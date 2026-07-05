using System;
using System.ComponentModel;

public class CarStateViewModel : INotifyPropertyChanged
{
    private const double PedalMaxAngle = 40.0;

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private double _acceleratorPercentage;
    public double AcceleratorPercentage
    {
        get => _acceleratorPercentage;
        set
        {
            _acceleratorPercentage = value;
            AcceleratorAngle = PercentageToPedalAngle(value);
            OnPropertyChanged(nameof(AcceleratorPercentage));
        }
    }

    private double _brakePercentage;
    public double BrakePercentage
    {
        get => _brakePercentage;
        set
        {
            _brakePercentage = value;
            BrakeAngle = PercentageToPedalAngle(value);
            OnPropertyChanged(nameof(BrakePercentage));
        }
    }

    private double _acceleratorAngle;
    public double AcceleratorAngle
    {
        get => _acceleratorAngle;
        set { _acceleratorAngle = value; OnPropertyChanged(nameof(AcceleratorAngle)); }
    }

    private double _brakeAngle;
    public double BrakeAngle
    {
        get => _brakeAngle;
        set { _brakeAngle = value; OnPropertyChanged(nameof(BrakeAngle)); }
    }

    private bool _botsStatus;
    public bool BOTSStatus
    {
        get => _botsStatus;
        set { _botsStatus = value; OnPropertyChanged(nameof(BOTSStatus)); }
    }

    private bool _appsStatus;
    public bool APPSStatus
    {
        get => _appsStatus;
        set { _appsStatus = value; OnPropertyChanged(nameof(APPSStatus)); }
    }

    private double _motorRpm;
    public double MotorRpm
    {
        get => _motorRpm;
        set { _motorRpm = value; OnPropertyChanged(nameof(MotorRpm)); }
    }

    private double _torqueCommand;
    public double TorqueCommand
    {
        get => _torqueCommand;
        set { _torqueCommand = value; OnPropertyChanged(nameof(TorqueCommand)); }
    }

    private double _speedCommand;
    public double SpeedCommand
    {
        get => _speedCommand;
        set { _speedCommand = value; OnPropertyChanged(nameof(SpeedCommand)); }
    }

    private double _accumulatorVoltage;
    public double AccumulatorVoltage
    {
        get => _accumulatorVoltage;
        set
        {
            _accumulatorVoltage = value;
            OnPropertyChanged(nameof(AccumulatorVoltage));
            OnPropertyChanged(nameof(PackPower));
            OnPropertyChanged(nameof(AccumulatorStatus));
        }
    }

    private double _accumulatorCurrent;
    public double AccumulatorCurrent
    {
        get => _accumulatorCurrent;
        set
        {
            _accumulatorCurrent = value;
            OnPropertyChanged(nameof(AccumulatorCurrent));
            OnPropertyChanged(nameof(PackPower));
            OnPropertyChanged(nameof(AccumulatorStatus));
        }
    }

    public double PackPower => AccumulatorVoltage * AccumulatorCurrent / 1000.0;
    public string AccumulatorStatus => AccumulatorVoltage > 0
        ? $"Pack active - {AccumulatorVoltage:F1} V, {AccumulatorCurrent:F1} A"
        : "Waiting for accumulator data";

    private double _maxCellTemp;
    public double MaxCellTemp
    {
        get => _maxCellTemp;
        set
        {
            _maxCellTemp = value;
            OnPropertyChanged(nameof(MaxCellTemp));
            OnPropertyChanged(nameof(HottestCellInfo));
        }
    }

    private double _minCellTemp;
    public double MinCellTemp
    {
        get => _minCellTemp;
        set { _minCellTemp = value; OnPropertyChanged(nameof(MinCellTemp)); }
    }

    private double _avgCellTemp;
    public double AvgCellTemp
    {
        get => _avgCellTemp;
        set { _avgCellTemp = value; OnPropertyChanged(nameof(AvgCellTemp)); }
    }

    private int _hottestCellId;
    public int HottestCellId
    {
        get => _hottestCellId;
        set
        {
            _hottestCellId = value;
            OnPropertyChanged(nameof(HottestCellId));
            OnPropertyChanged(nameof(HottestCellInfo));
        }
    }

    public string HottestCellInfo => MaxCellTemp > 0
        ? $"Hottest cell: #{HottestCellId} at {MaxCellTemp:F1} °C"
        : "Waiting for cell temperature data";

    // Wheel speeds
    private double _flWheelspeed;
    public double FLWheelspeed
    {
        get => _flWheelspeed;
        set { _flWheelspeed = value; OnPropertyChanged(nameof(FLWheelspeed)); }
    }

    private double _frWheelspeed;
    public double FRWheelspeed
    {
        get => _frWheelspeed;
        set { _frWheelspeed = value; OnPropertyChanged(nameof(FRWheelspeed)); }
    }

    private double _rlWheelspeed;
    public double RLWheelspeed
    {
        get => _rlWheelspeed;
        set { _rlWheelspeed = value; OnPropertyChanged(nameof(RLWheelspeed)); }
    }

    private double _rrWheelspeed;
    public double RRWheelspeed
    {
        get => _rrWheelspeed;
        set { _rrWheelspeed = value; OnPropertyChanged(nameof(RRWheelspeed)); }
    }

    // Brake temperatures
    private double _flBrakeTemp;
    public double FLBrakeTemp
    {
        get => _flBrakeTemp;
        set { _flBrakeTemp = value; OnPropertyChanged(nameof(FLBrakeTemp)); }
    }

    private double _frBrakeTemp;
    public double FRBrakeTemp
    {
        get => _frBrakeTemp;
        set { _frBrakeTemp = value; OnPropertyChanged(nameof(FRBrakeTemp)); }
    }

    private double _rlBrakeTemp;
    public double RLBrakeTemp
    {
        get => _rlBrakeTemp;
        set { _rlBrakeTemp = value; OnPropertyChanged(nameof(RLBrakeTemp)); }
    }

    private double _rrBrakeTemp;
    public double RRBrakeTemp
    {
        get => _rrBrakeTemp;
        set { _rrBrakeTemp = value; OnPropertyChanged(nameof(RRBrakeTemp)); }
    }

    // Energy setting
    private int _energySetting;
    public int EnergySetting
    {
        get => _energySetting;
        set { _energySetting = value; OnPropertyChanged(nameof(EnergySetting)); }
    }

    // SDC placeholders
    private bool _imdOk;
    public bool ImdOk
    {
        get => _imdOk;
        set { _imdOk = value; OnPropertyChanged(nameof(ImdOk)); }
    }

    private bool _bmsOk;
    public bool BmsOk
    {
        get => _bmsOk;
        set { _bmsOk = value; OnPropertyChanged(nameof(BmsOk)); }
    }

    private bool _estopOk;
    public bool EstopOk
    {
        get => _estopOk;
        set { _estopOk = value; OnPropertyChanged(nameof(EstopOk)); }
    }

    private bool _inertiaOk;
    public bool InertiaOk
    {
        get => _inertiaOk;
        set { _inertiaOk = value; OnPropertyChanged(nameof(InertiaOk)); }
    }

    private bool _brakeOvertravelOk;
    public bool BrakeOvertravelOk
    {
        get => _brakeOvertravelOk;
        set { _brakeOvertravelOk = value; OnPropertyChanged(nameof(BrakeOvertravelOk)); }
    }

    private bool _amsOk;
    public bool AmsOk
    {
        get => _amsOk;
        set { _amsOk = value; OnPropertyChanged(nameof(AmsOk)); }
    }

    private bool _lvmsOk;
    public bool LvmsOk
    {
        get => _lvmsOk;
        set { _lvmsOk = value; OnPropertyChanged(nameof(LvmsOk)); }
    }

    private bool _bspdOk;
    public bool BspdOk
    {
        get => _bspdOk;
        set { _bspdOk = value; OnPropertyChanged(nameof(BspdOk)); }
    }

    private bool _killSwitchOk;
    public bool KillSwitchOk
    {
        get => _killSwitchOk;
        set { _killSwitchOk = value; OnPropertyChanged(nameof(KillSwitchOk)); }
    }

    private bool _botsOk;
    public bool BotsOk
    {
        get => _botsOk;
        set { _botsOk = value; OnPropertyChanged(nameof(BotsOk)); }
    }

    private bool _appsOk;
    public bool AppsOk
    {
        get => _appsOk;
        set { _appsOk = value; OnPropertyChanged(nameof(AppsOk)); }
    }

    private bool _lhsSbOk;
    public bool LhsSbOk
    {
        get => _lhsSbOk;
        set { _lhsSbOk = value; OnPropertyChanged(nameof(LhsSbOk)); }
    }

    private bool _rhsSbOk;
    public bool RhsSbOk
    {
        get => _rhsSbOk;
        set { _rhsSbOk = value; OnPropertyChanged(nameof(RhsSbOk)); }
    }

    private bool _latchboardOk;
    public bool LatchboardOk
    {
        get => _latchboardOk;
        set { _latchboardOk = value; OnPropertyChanged(nameof(LatchboardOk)); }
    }

    private bool _dcOk;
    public bool DcOk
    {
        get => _dcOk;
        set { _dcOk = value; OnPropertyChanged(nameof(DcOk)); }
    }

    private bool _hvdOk;
    public bool HvdOk
    {
        get => _hvdOk;
        set { _hvdOk = value; OnPropertyChanged(nameof(HvdOk)); }
    }

    private bool _mcOk;
    public bool McOk
    {
        get => _mcOk;
        set { _mcOk = value; OnPropertyChanged(nameof(McOk)); }
    }

    private bool _selfLatchOk;
    public bool SelfLatchOk
    {
        get => _selfLatchOk;
        set { _selfLatchOk = value; OnPropertyChanged(nameof(SelfLatchOk)); }
    }

    private bool _tsalSwitchOk;
    public bool TsalSwitchOk
    {
        get => _tsalSwitchOk;
        set { _tsalSwitchOk = value; OnPropertyChanged(nameof(TsalSwitchOk)); }
    }

    private bool _tsmsOk;
    public bool TsmsOk
    {
        get => _tsmsOk;
        set { _tsmsOk = value; OnPropertyChanged(nameof(TsmsOk)); }
    }

    private bool _prechargeOk;
    public bool PrechargeOk
    {
        get => _prechargeOk;
        set { _prechargeOk = value; OnPropertyChanged(nameof(PrechargeOk)); }
    }

    private static double PercentageToPedalAngle(double percentage)
    {
        double clamped = Math.Max(0, Math.Min(100, percentage));
        return clamped / 100.0 * PedalMaxAngle;
    }
}
