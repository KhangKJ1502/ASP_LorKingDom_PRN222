using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.DTOs
{
    public record ConversationDto(
     string ConversationId,
     string CustomerUserId,
     string StaffUserId,
     string? LastMessage,
     DateTime? LastAt,
     int UnreadForStaff,
     bool CustomerOnline
 );
}
