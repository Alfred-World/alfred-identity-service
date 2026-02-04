using Alfred.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alfred.Identity.Infrastructure.Providers.PostgreSQL.EntityConfigurations;

public class UserBanConfiguration : IEntityTypeConfiguration<UserBan>
{
    public void Configure(EntityTypeBuilder<UserBan> builder)
    {
        builder.ToTable("user_bans");

        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.User)
            .WithMany(u => u.UserBans)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
