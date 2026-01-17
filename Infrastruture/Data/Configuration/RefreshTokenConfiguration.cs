using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Data.Configuration
{
    public class RefreshTokenConfiguration:IEntityTypeConfiguration<Core.Entities.RefreshToken>
    {
        public void Configure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<Core.Entities.RefreshToken> builder)
        {
            builder.ToTable("RefreshTokens");          
        }        
    }
}
