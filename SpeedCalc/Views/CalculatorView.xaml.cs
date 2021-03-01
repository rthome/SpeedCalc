using ReactiveUI;

using SpeedCalc.ViewModels;

namespace SpeedCalc.Views
{
    public partial class CalculatorView : ReactiveWindow<CalculatorViewModel>
    {
        public CalculatorView(CalculatorViewModel viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;

            this.WhenActivated(disposables =>
            {

            });
        }
    }
}
