using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.DTOs
{
    public record MessageDto(string Id, string ConversationId, string SenderId, string Text, DateTime SentAt);
}
