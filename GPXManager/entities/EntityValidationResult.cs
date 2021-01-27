using System.Collections.Generic;

namespace GPXManager.entities
{
    public class EntityValidationResult
    {
        public EntityValidationResult()
        {
            _entityValidationMessages = new List<EntityValidationMessage>();
            ErrorMessage = "";
            WarningMessage = "";
            InfoMessage = "";
        }

        public string ErrorMessage { get; private set; }
        public string WarningMessage { get; private set; }
        public string InfoMessage { get; private set; }

        public int MessageCount { get { return _entityValidationMessages.Count; } }

        public void AddMessage(string message, MessageType messageType = MessageType.Error)
        {
            AddMessage(new EntityValidationMessage($"{message}\r\n", messageType));
        }

        public void AddMessage(EntityValidationMessage message)
        {
            _entityValidationMessages.Add(message);
            if (message.MessageType == MessageType.Error)
            {
                ErrorMessage += $"{ message.Message}\n";
            }
            else if (message.MessageType == MessageType.Information)
            {
                InfoMessage += $"{message.Message}\n";
            }
            else if (message.MessageType == MessageType.Warning)
            {
                WarningMessage += $"{message.Message}\n";
            }
        }

        public bool NSAPRegionGearRequireUpdate { get; set; }
        public bool EntityWasEdited { get; set; }
        private List<EntityValidationMessage> _entityValidationMessages { get; }
    }
}