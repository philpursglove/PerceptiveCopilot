using Microsoft.Data.SqlTypes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PerceptiveCopilot.Data
{
    public class DocumentChunk
    {
        [Key] 
        public int Id { get; set; }

        public string DocumentName { get; set; } = string.Empty;
        public string ContentChunk { get; set; } = string.Empty;
        [Column(TypeName = "vector(1536)")]
        public SqlVector<float> Embedding { get; set; }
    }
}
