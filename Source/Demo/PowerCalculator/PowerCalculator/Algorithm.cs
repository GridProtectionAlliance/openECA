using GSF;
using GSF.Units;
using PowerCalculator.Framework;
using PowerCalculator.Model.Test;
using Power = PowerCalculator.Model.Test.Power;

namespace PowerCalculator
{
    static class Algorithm
    {
		public static Power Execute(VIPair input)
		{
			Power output = new Power();
            ComplexNumber voltage = ToComplex(input.Voltage);
            ComplexNumber current = ToComplex(input.Current);

            output.Real = 3 * (voltage.Real * current.Real + voltage.Imaginary * current.Imaginary) / SI.Mega;
            output.Reactive = 3 * (voltage.Imaginary * current.Real - voltage.Real * current.Imaginary) / SI.Mega;

            string real = output.Real.ToString("0.000").PadLeft(10);
            string reactive = output.Reactive.ToString("0.000").PadLeft(10);
            MainWindow.WriteMessage($"MW: {real}  MVAR: {reactive}");

            return output;
		}

        static ComplexNumber ToComplex(Phasor phasor)
        {
            Angle angle = Angle.FromDegrees(phasor.Angle);
            double magnitude = phasor.Magnitude;
            return new ComplexNumber(angle, magnitude);
        }
    }
}
