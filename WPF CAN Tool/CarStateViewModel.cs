using System;
using System.ComponentModel;

public class CarStateViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private double _acceleratorPercentage;
    public double AcceleratorPercentage
    {
        get => _acceleratorPercentage;
        set { _acceleratorPercentage = value; OnPropertyChanged(nameof(AcceleratorPercentage)); }
    }

    private double _brakePercentage;
    public double BrakePercentage
    {
        get => _brakePercentage;
        set { _brakePercentage = value; OnPropertyChanged(nameof(BrakePercentage)); }
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

    private double _accumulatorVoltage;
    public double AccumulatorVoltage
    {
        get => _accumulatorVoltage;
        set { _accumulatorVoltage = value; OnPropertyChanged(nameof(AccumulatorVoltage)); }
    }

    private double _accumulatorCurrent;
    public double AccumulatorCurrent
    {
        get => _accumulatorCurrent;
        set { _accumulatorCurrent = value; OnPropertyChanged(nameof(AccumulatorCurrent)); }
    }

    private double _maxCellTemp;
    public double MaxCellTemp
    {
        get => _maxCellTemp;
        set { _maxCellTemp = value; OnPropertyChanged(nameof(MaxCellTemp)); }
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
}
