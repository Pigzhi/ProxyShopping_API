using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace API大專.Models;

public partial class ProxyContext : DbContext
{
    public ProxyContext()
    {
    }

    public ProxyContext(DbContextOptions<ProxyContext> options)
        : base(options)
    {
    }
    public virtual DbSet<Commission> Commissions { get; set; }

    public virtual DbSet<CommissionHistory> CommissionHistories { get; set; }

    public virtual DbSet<CommissionOrder> CommissionOrders { get; set; }

    public virtual DbSet<User> Users { get; set; }
    public virtual DbSet<CommissionReceipt> CommissionReceipts { get; set; }

    public virtual DbSet<CommissionShipping> CommissionShippings { get; set; }

    public virtual DbSet<CommissionSequence> CommissionSequences { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Commission>(entity =>
        {
            entity.HasKey(e => e.CommissionId).HasName("PK__Commissi__D19D7CC9A721000E");

            entity.ToTable("Commission");

            entity.Property(e => e.CommissionId).HasColumnName("commission_id");
            entity.Property(e => e.Category)
                .HasMaxLength(50)
                .HasColumnName("category");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatorId)
                .HasMaxLength(50)
                .HasColumnName("creator_id");
            entity.Property(e => e.Deadline)
                .HasColumnType("datetime")
                .HasColumnName("deadline");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.EscrowAmount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("escrowAmount");
            entity.Property(e => e.FailCount).HasColumnName("fail_count");
            entity.Property(e => e.Fee)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("fee");
            entity.Property(e => e.ImageUrl).HasColumnName("image_url");
            entity.Property(e => e.Location)
                .HasMaxLength(255)
                .HasColumnName("location");
            entity.Property(e => e.Price)
                .HasColumnType("decimal(15, 2)")
                .HasColumnName("price");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.Status)
                .HasMaxLength(15)
                .HasDefaultValue("未審核")
                .HasColumnName("status");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("title");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.Creator).WithMany(p => p.Commissions)
                .HasForeignKey(d => d.CreatorId)
                .HasConstraintName("FK_Commission_Users_Creator");
        });

        modelBuilder.Entity<CommissionHistory>(entity =>
        {
            entity.HasKey(e => e.HistoryId).HasName("PK__Commissi__096AA2E939BE86B4");

            entity.ToTable("CommissionHistory");

            entity.Property(e => e.HistoryId).HasColumnName("history_id");
            entity.Property(e => e.Action)
                .HasMaxLength(50)
                .HasColumnName("action");
            entity.Property(e => e.ChangedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("changed_at");
            entity.Property(e => e.ChangedBy)
                .HasMaxLength(50)
                .HasColumnName("changed_by");
            entity.Property(e => e.CommissionId).HasColumnName("commission_id");
            entity.Property(e => e.NewData).HasColumnName("new_data");
            entity.Property(e => e.OldData).HasColumnName("old_data");

            entity.HasOne(d => d.Commission).WithMany(p => p.CommissionHistories)
                .HasForeignKey(d => d.CommissionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CommissionHistory_Commission");
        });

        modelBuilder.Entity<CommissionOrder>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("PK__Order__4659622962A69E3E");

            entity.ToTable("CommissionOrder");

            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.Amount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("amount");
            entity.Property(e => e.BuyerId)
                .HasMaxLength(50)
                .HasColumnName("buyer_id");
            entity.Property(e => e.CommissionId).HasColumnName("commission_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.FinishedAt)
                .HasColumnType("datetime")
                .HasColumnName("finished_at");
            entity.Property(e => e.SellerId)
                .HasMaxLength(50)
                .HasColumnName("seller_id");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasColumnName("status");

            entity.HasOne(d => d.Commission).WithMany(p => p.CommissionOrders)
                .HasForeignKey(d => d.CommissionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Order_Commission");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Uid).HasName("PK__Users__DD701264B453AD75");

            entity.HasIndex(e => e.Email, "UQ__Users__AB6E616411AF85A7").IsUnique();

            entity.Property(e => e.Uid)
                .HasMaxLength(50)
                .HasColumnName("uid");
            entity.Property(e => e.Balance)
                .HasDefaultValue(0.00m)
                .HasColumnType("decimal(15, 2)")
                .HasColumnName("balance");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .HasColumnName("email");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasColumnName("password_hash");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("phone");
        });
        modelBuilder.Entity<CommissionReceipt>(entity =>
        {
            entity.HasKey(e => e.ReceiptId).HasName("PK__Commissi__91F52C1FCE4391C9");

            entity.ToTable("CommissionReceipt");

            entity.Property(e => e.ReceiptId).HasColumnName("receipt_id");
            entity.Property(e => e.CommissionId).HasColumnName("commission_id");
            entity.Property(e => e.ReceiptAmount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("receipt_amount");
            entity.Property(e => e.ReceiptDate)
                .HasColumnType("datetime")
                .HasColumnName("receipt_date");
            entity.Property(e => e.ReceiptImageUrl).HasColumnName("receipt_image_url");
            entity.Property(e => e.Remark)
                .HasMaxLength(500)
                .HasColumnName("remark");
            entity.Property(e => e.UploadedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("uploaded_at");
            entity.Property(e => e.UploadedBy)
                .HasMaxLength(50)
                .HasColumnName("uploaded_by");
        });

        modelBuilder.Entity<CommissionShipping>(entity =>
        {
            entity.HasKey(e => e.ShippingId).HasName("PK__Commissi__059B15A9A6D5156F");

            entity.ToTable("CommissionShipping");

            entity.HasIndex(e => e.CommissionId, "UQ_CommissionShipping").IsUnique();

            entity.Property(e => e.ShippingId).HasColumnName("shipping_id");
            entity.Property(e => e.CommissionId).HasColumnName("commission_id");
            entity.Property(e => e.LogisticsName)
                .HasMaxLength(50)
                .HasColumnName("logistics_name");
            entity.Property(e => e.Remark)
                .HasMaxLength(255)
                .HasColumnName("remark");
            entity.Property(e => e.ShippedAt)
                .HasColumnType("datetime")
                .HasColumnName("shipped_at");
            entity.Property(e => e.ShippedBy)
                .HasMaxLength(50)
                .HasColumnName("shipped_by");
            entity.Property(e => e.Status)
                .HasMaxLength(10)
                .HasColumnName("status");
            entity.Property(e => e.TrackingNumber)
                .HasMaxLength(100)
                .HasColumnName("tracking_number");
        });
        modelBuilder.Entity<CommissionSequence>(entity =>
        {
            entity.HasKey(e => e.Ym);

            entity.ToTable("commission_sequence");

            entity.Property(e => e.Ym)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("ym");
            entity.Property(e => e.Seq).HasColumnName("seq");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("updated_at");
        });
        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
