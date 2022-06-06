namespace ArcGISRuntimeDesktop.Controls
{
    public sealed partial class AppDialog : UserControl
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public static AppDialog Instance { get; private set; }
        public AppDialog()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            this.InitializeComponent();
            this.Visibility = Visibility.Collapsed;
            Instance = this;
        }

        TaskCompletionSource<bool> _taskCompletionSource;

        public Task ShowAsync(string title, UIElement content)
        {
            if (_taskCompletionSource != null)
                _taskCompletionSource.TrySetResult(false);
            _taskCompletionSource = new TaskCompletionSource<bool>();
            Title.Text = title;
            ContentArea.Content = content;
            this.Visibility = Visibility.Visible;
            VisualStateManager.GoToState(this, "Show", true);
            return _taskCompletionSource.Task;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(this, "Hide", true);
            _taskCompletionSource.TrySetResult(false);
            this.Visibility = Visibility.Collapsed;
        }
    }
}
