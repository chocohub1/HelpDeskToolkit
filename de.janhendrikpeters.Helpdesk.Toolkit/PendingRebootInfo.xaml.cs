using System.Windows;
using de.janhendrikpeters.helpdesk.library;

namespace de.janhendrikpeters.helpdesk.toolkit
{
    /// <summary>
    /// Interaction logic for PendingRebootInfo.xaml
    /// </summary>
    public partial class PendingRebootInfo : Window
    {
        public PendingRebootInfo()
        {
            InitializeComponent();
            IsCcmRebootCheckBox.IsChecked = Helper.Instance.CurrentPendingReboot.IsCcmReboot;
            IsWindowsUpdateRebootCheckBox.IsChecked = Helper.Instance.CurrentPendingReboot.IsWindowsUpdateReboot;
            RebootPendingCheckBox.IsChecked = Helper.Instance.CurrentPendingReboot.RebootPending;
            PendingFileRenamesTextBox.Text = Helper.Instance.CurrentPendingReboot.PendingFileRenames;
        }
    }
}
