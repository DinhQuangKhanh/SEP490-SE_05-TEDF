using TEDF.Domain.Common.Primitives;

namespace TEDF.Domain.Aggregates.ProjectAggregate.Entities
{
    /// <summary>
    /// A student the proposing mentor listed on the capstone register form attached to the topic.
    /// The roster is only an intent: the real <c>Group</c> is materialized after the topic passes
    /// evaluation. An empty roster means the topic follows the normal pool flow.
    /// </summary>
    public class ProposedGroupMember : Entity<int>
    {
        /// <summary>
        /// Gets the project (topic) this roster entry belongs to.
        /// </summary>
        public Guid ProjectId { get; private set; }

        /// <summary>
        /// Gets the student (user) identifier.
        /// </summary>
        public Guid StudentId { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this student is the group leader
        /// (the "Leader" row on the register form).
        /// </summary>
        public bool IsLeader { get; private set; }

        private ProposedGroupMember() { }

        internal static ProposedGroupMember Create(Guid projectId, Guid studentId, bool isLeader)
        {
            return new ProposedGroupMember
            {
                ProjectId = projectId,
                StudentId = studentId,
                IsLeader = isLeader
            };
        }
    }
}
