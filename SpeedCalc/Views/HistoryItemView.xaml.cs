using ReactiveUI;

using SpeedCalc.ViewModels;

namespace SpeedCalc.Views
{
    public partial class HistoryItemView : ReactiveUserControl<HistoryItemViewModel>
    {
        public HistoryItemView(HistoryItemViewModel viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;

            this.WhenActivated(disposables =>
            {

            });
        }
    }
}
