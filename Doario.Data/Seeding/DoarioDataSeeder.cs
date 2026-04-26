using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Doario.Data.Models.Lookups;
using Microsoft.EntityFrameworkCore;

namespace Doario.Data.Seeding
{
    public class DoarioDataSeeder
    {
        private static readonly DateTime Epoch = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime Never = new DateTime(9999, 12, 31, 0, 0, 0, DateTimeKind.Utc);

        public static void Seed(ModelBuilder modelBuilder)
        {
            SeedDocumentStatuses(modelBuilder);
            SeedAssignmentTypes(modelBuilder);
            SeedNotificationTypes(modelBuilder);
            SeedSourceTypes(modelBuilder);
            SeedSystemStatuses(modelBuilder);
            SeedMessageTypes(modelBuilder);
        }

        private static void SeedDocumentStatuses(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DocumentStatus>().HasData(
                new DocumentStatus { DocumentStatusId = 1, Name = "Unassigned", SortOrder = 100, StartDate = Epoch, EndDate = Never },
                new DocumentStatus { DocumentStatusId = 2, Name = "Assigned", SortOrder = 200, StartDate = Epoch, EndDate = Never },
                new DocumentStatus { DocumentStatusId = 3, Name = "Read", SortOrder = 300, StartDate = Epoch, EndDate = Never },
                new DocumentStatus { DocumentStatusId = 4, Name = "Actioned", SortOrder = 400, StartDate = Epoch, EndDate = Never },
                // Day 9
                new DocumentStatus { DocumentStatusId = 5, Name = "OcrFailed", SortOrder = 500, StartDate = Epoch, EndDate = Never },
                new DocumentStatus { DocumentStatusId = 6, Name = "EmailReceived", SortOrder = 600, StartDate = Epoch, EndDate = Never }
            );
        }

        private static void SeedAssignmentTypes(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AssignmentType>().HasData(
                new AssignmentType { AssignmentTypeId = 1, Name = "Primary", SortOrder = 100 },
                new AssignmentType { AssignmentTypeId = 2, Name = "CC", SortOrder = 200 }
            );
        }

        private static void SeedNotificationTypes(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<NotificationType>().HasData(
                new NotificationType { NotificationTypeId = 1, Name = "NewDocument", SortOrder = 100 },
                new NotificationType { NotificationTypeId = 2, Name = "Urgent", SortOrder = 200 },
                new NotificationType { NotificationTypeId = 3, Name = "Forwarded", SortOrder = 300 },
                new NotificationType { NotificationTypeId = 4, Name = "MessageReceived", SortOrder = 400 }
            );
        }

        private static void SeedSourceTypes(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SourceType>().HasData(
                new SourceType { SourceTypeId = 1, Name = "CSV", SortOrder = 100, StartDate = Epoch, EndDate = Never },
                new SourceType { SourceTypeId = 2, Name = "MicrosoftGraph", SortOrder = 200, StartDate = Epoch, EndDate = Never },
                new SourceType { SourceTypeId = 3, Name = "GoogleDirectory", SortOrder = 300, StartDate = Epoch, EndDate = Never },
                new SourceType { SourceTypeId = 4, Name = "Connector", SortOrder = 400, StartDate = Epoch, EndDate = Never },
                new SourceType { SourceTypeId = 5, Name = "SalesforceAPI", SortOrder = 500, StartDate = Epoch, EndDate = Never },
                new SourceType { SourceTypeId = 6, Name = "PowerSchoolAPI", SortOrder = 600, StartDate = Epoch, EndDate = Never },
                new SourceType { SourceTypeId = 7, Name = "CustomDatabase", SortOrder = 700, StartDate = Epoch, EndDate = Never },
                new SourceType { SourceTypeId = 8, Name = "CustomAPI", SortOrder = 800, StartDate = Epoch, EndDate = Never },
                new SourceType { SourceTypeId = 9, Name = "VirtualMailroom", SortOrder = 900, StartDate = Epoch, EndDate = Never },
                // Day 9
                new SourceType { SourceTypeId = 10, Name = "Fax", SortOrder = 1000, StartDate = Epoch, EndDate = Never },
                new SourceType { SourceTypeId = 11, Name = "Email", SortOrder = 1100, StartDate = Epoch, EndDate = Never }
            );
        }

        private static void SeedSystemStatuses(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SystemStatus>().HasData(
                new SystemStatus { SystemStatusId = 1, Name = "Active", SortOrder = 100, StartDate = Epoch, EndDate = Never },
                new SystemStatus { SystemStatusId = 2, Name = "Expired", SortOrder = 200, StartDate = Epoch, EndDate = Never },
                new SystemStatus { SystemStatusId = 3, Name = "Revoked", SortOrder = 300, StartDate = Epoch, EndDate = Never },
                new SystemStatus { SystemStatusId = 4, Name = "Success", SortOrder = 400, StartDate = Epoch, EndDate = Never },
                new SystemStatus { SystemStatusId = 5, Name = "Failed", SortOrder = 500, StartDate = Epoch, EndDate = Never },
                new SystemStatus { SystemStatusId = 6, Name = "PartialSuccess", SortOrder = 600, StartDate = Epoch, EndDate = Never },
                new SystemStatus { SystemStatusId = 7, Name = "Pending", SortOrder = 700, StartDate = Epoch, EndDate = Never },
                new SystemStatus { SystemStatusId = 8, Name = "Sent", SortOrder = 800, StartDate = Epoch, EndDate = Never }
            );
        }

        private static void SeedMessageTypes(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MessageType>().HasData(
                new MessageType { MessageTypeId = 1, Name = "Note", SortOrder = 100 },
                new MessageType { MessageTypeId = 2, Name = "ActionedViaOutlook", SortOrder = 200 },
                new MessageType { MessageTypeId = 3, Name = "ActionedViaPortal", SortOrder = 300 },
                new MessageType { MessageTypeId = 4, Name = "Forwarded", SortOrder = 400 },
                new MessageType { MessageTypeId = 5, Name = "AdminConfirmedViaOutlook", SortOrder = 500 },
                new MessageType { MessageTypeId = 6, Name = "AdminReassignedViaOutlook", SortOrder = 600 },
                new MessageType { MessageTypeId = 7, Name = "AdminMarkedUrgent", SortOrder = 700 },
                new MessageType { MessageTypeId = 8, Name = "ViewedDocument", SortOrder = 800 },
                // Day 9
                new MessageType { MessageTypeId = 9, Name = "FaxReceived", SortOrder = 900 },
                new MessageType { MessageTypeId = 10, Name = "EmailReceived", SortOrder = 1000 }
            );
        }
    }
}