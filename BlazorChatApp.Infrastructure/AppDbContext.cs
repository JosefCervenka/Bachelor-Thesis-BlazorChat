using BlazorChatApp.Domain.Models.ChatMembers;
using BlazorChatApp.Domain.Models.ChatRequests;
using BlazorChatApp.Domain.Models.Chats;
using BlazorChatApp.Domain.Models.Messages;
using BlazorChatApp.Domain.Models.Users;
using Microsoft.EntityFrameworkCore;

namespace BlazorChatApp.Infrastructure;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<ChatRoom> ChatRooms { get; set; }
    public DbSet<DirectChatRoom> DirectChatRooms { get; set; }
    public DbSet<GroupChatRoom> GroupChatRooms { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<PhotoMessage> PhotoMessages { get; set; }
    public DbSet<ChatMember> ChatMembers { get; set; }
    public DbSet<GroupChatMember> GroupChatMembers { get; set; }
    public DbSet<ChatMemberRole> ChatMemberRoles { get; set; }
    public DbSet<ChatRoom> Chats { get; set; }
    public DbSet<ChatRequest> ChatRequests { get; set; }
    public DbSet<FriendRequest> FriendRequests { get; set; }
    public DbSet<GroupChatRequest> GroupChatRequests { get; set; }
    public DbSet<ChatRequestStatus> ChatRequestStatuses { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        #region User
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.UserName)
            .IsUnique();
        #endregion

        #region ChatRequestStatus
        modelBuilder.Entity<ChatRequestStatus>().HasData(
            new ChatRequestStatus { Id = (int)ChatRequestStatusEnum.Created, StatusCode = "Created" },
            new ChatRequestStatus { Id = (int)ChatRequestStatusEnum.Accepted, StatusCode = "Accepted" },
            new ChatRequestStatus { Id = (int)ChatRequestStatusEnum.Decline, StatusCode = "Decline" }
        );
        #endregion

        #region ChatMemberRole
        modelBuilder.Entity<ChatMemberRole>().HasData(
            new ChatMemberRole { Id = (int)ChatMemberRoleEnum.Admin, Name = "Admin" },
            new ChatMemberRole { Id = (int)ChatMemberRoleEnum.User, Name = "User" },
            new ChatMemberRole { Id = (int)ChatMemberRoleEnum.Owner, Name = "Owner" }
        );
        #endregion
    }
}