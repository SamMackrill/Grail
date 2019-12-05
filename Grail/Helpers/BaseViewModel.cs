using System.Collections;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;

using Grail.Model;

using JetBrains.Annotations;


namespace Grail
{
    public abstract class BaseViewModel: Keyed, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        public RelayCommand<IList> CopyToClipboardCommand => new RelayCommand<IList>((list) =>
        {
            if (list == null) return;
            var sb = new StringBuilder();

            foreach (var item in list)
            {
                sb.AppendLine(item.ToString());
            }

            Clipboard.SetDataObject(sb.ToString());
        });
    }


}
