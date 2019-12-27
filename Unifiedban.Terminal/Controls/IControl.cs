using System;
using System.Collections.Generic;
using System.Text;
using Telegram.Bot.Types;

namespace Unifiedban.Terminal.Controls
{
    public interface IControl
    {
        public enum ControlResultType
        {
            positive,
            negative,
            skipped
        }
        ControlResult DoCheck(Message message);
    }

    public class ControlResult
    {
        public IControl.ControlResultType Result { get; set; }
        public string CheckName { get; set; }
    }
}
