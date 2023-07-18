﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using RdtClient.Data.Data;

#nullable disable

namespace RdtClient.Data.Migrations
{
    [DbContext(typeof(DataContext))]
    [Migration("20230718151649_Downloads_Add_Folder")]
    partial class Downloads_Add_Folder
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "6.0.15");

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRole", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("TEXT");

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken()
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .HasMaxLength(256)
                        .HasColumnType("TEXT");

                    b.Property<string>("NormalizedName")
                        .HasMaxLength(256)
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("NormalizedName")
                        .IsUnique()
                        .HasDatabaseName("RoleNameIndex");

                    b.ToTable("AspNetRoles", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("ClaimType")
                        .HasColumnType("TEXT");

                    b.Property<string>("ClaimValue")
                        .HasColumnType("TEXT");

                    b.Property<string>("RoleId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("RoleId");

                    b.ToTable("AspNetRoleClaims", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUser", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("TEXT");

                    b.Property<int>("AccessFailedCount")
                        .HasColumnType("INTEGER");

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken()
                        .HasColumnType("TEXT");

                    b.Property<string>("Email")
                        .HasMaxLength(256)
                        .HasColumnType("TEXT");

                    b.Property<bool>("EmailConfirmed")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("LockoutEnabled")
                        .HasColumnType("INTEGER");

                    b.Property<DateTimeOffset?>("LockoutEnd")
                        .HasColumnType("TEXT");

                    b.Property<string>("NormalizedEmail")
                        .HasMaxLength(256)
                        .HasColumnType("TEXT");

                    b.Property<string>("NormalizedUserName")
                        .HasMaxLength(256)
                        .HasColumnType("TEXT");

                    b.Property<string>("PasswordHash")
                        .HasColumnType("TEXT");

                    b.Property<string>("PhoneNumber")
                        .HasColumnType("TEXT");

                    b.Property<bool>("PhoneNumberConfirmed")
                        .HasColumnType("INTEGER");

                    b.Property<string>("SecurityStamp")
                        .HasColumnType("TEXT");

                    b.Property<bool>("TwoFactorEnabled")
                        .HasColumnType("INTEGER");

                    b.Property<string>("UserName")
                        .HasMaxLength(256)
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("NormalizedEmail")
                        .HasDatabaseName("EmailIndex");

                    b.HasIndex("NormalizedUserName")
                        .IsUnique()
                        .HasDatabaseName("UserNameIndex");

                    b.ToTable("AspNetUsers", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<string>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("ClaimType")
                        .HasColumnType("TEXT");

                    b.Property<string>("ClaimValue")
                        .HasColumnType("TEXT");

                    b.Property<string>("UserId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("AspNetUserClaims", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<string>", b =>
                {
                    b.Property<string>("LoginProvider")
                        .HasColumnType("TEXT");

                    b.Property<string>("ProviderKey")
                        .HasColumnType("TEXT");

                    b.Property<string>("ProviderDisplayName")
                        .HasColumnType("TEXT");

                    b.Property<string>("UserId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("LoginProvider", "ProviderKey");

                    b.HasIndex("UserId");

                    b.ToTable("AspNetUserLogins", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<string>", b =>
                {
                    b.Property<string>("UserId")
                        .HasColumnType("TEXT");

                    b.Property<string>("RoleId")
                        .HasColumnType("TEXT");

                    b.HasKey("UserId", "RoleId");

                    b.HasIndex("RoleId");

                    b.ToTable("AspNetUserRoles", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<string>", b =>
                {
                    b.Property<string>("UserId")
                        .HasColumnType("TEXT");

                    b.Property<string>("LoginProvider")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.Property<string>("Value")
                        .HasColumnType("TEXT");

                    b.HasKey("UserId", "LoginProvider", "Name");

                    b.ToTable("AspNetUserTokens", (string)null);
                });

            modelBuilder.Entity("RdtClient.Data.Models.Data.Download", b =>
                {
                    b.Property<Guid>("DownloadId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<DateTimeOffset>("Added")
                        .HasColumnType("TEXT");

                    b.Property<DateTimeOffset?>("Completed")
                        .HasColumnType("TEXT");

                    b.Property<DateTimeOffset?>("DownloadFinished")
                        .HasColumnType("TEXT");

                    b.Property<DateTimeOffset?>("DownloadQueued")
                        .HasColumnType("TEXT");

                    b.Property<DateTimeOffset?>("DownloadStarted")
                        .HasColumnType("TEXT");

                    b.Property<string>("Error")
                        .HasColumnType("TEXT");

                    b.Property<string>("Folder")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Link")
                        .HasColumnType("TEXT");

                    b.Property<string>("Path")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("RemoteId")
                        .HasColumnType("TEXT");

                    b.Property<int>("RetryCount")
                        .HasColumnType("INTEGER");

                    b.Property<Guid>("TorrentId")
                        .HasColumnType("TEXT");

                    b.Property<DateTimeOffset?>("UnpackingFinished")
                        .HasColumnType("TEXT");

                    b.Property<DateTimeOffset?>("UnpackingQueued")
                        .HasColumnType("TEXT");

                    b.Property<DateTimeOffset?>("UnpackingStarted")
                        .HasColumnType("TEXT");

                    b.HasKey("DownloadId");

                    b.HasIndex("TorrentId");

                    b.ToTable("Downloads");
                });

            modelBuilder.Entity("RdtClient.Data.Models.Data.Setting", b =>
                {
                    b.Property<string>("SettingId")
                        .HasColumnType("TEXT");

                    b.Property<string>("Value")
                        .HasColumnType("TEXT");

                    b.HasKey("SettingId");

                    b.ToTable("Settings");
                });

            modelBuilder.Entity("RdtClient.Data.Models.Data.Torrent", b =>
                {
                    b.Property<Guid>("TorrentId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<DateTimeOffset>("Added")
                        .HasColumnType("TEXT");

                    b.Property<string>("Category")
                        .HasColumnType("TEXT");

                    b.Property<DateTimeOffset?>("Completed")
                        .HasColumnType("TEXT");

                    b.Property<int>("DeleteOnError")
                        .HasColumnType("INTEGER");

                    b.Property<int>("DownloadAction")
                        .HasColumnType("INTEGER");

                    b.Property<string>("DownloadManualFiles")
                        .HasColumnType("TEXT");

                    b.Property<int>("DownloadMinSize")
                        .HasColumnType("INTEGER");

                    b.Property<int>("DownloadRetryAttempts")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Error")
                        .HasColumnType("TEXT");

                    b.Property<string>("FileOrMagnet")
                        .HasColumnType("TEXT");

                    b.Property<DateTimeOffset?>("FilesSelected")
                        .HasColumnType("TEXT");

                    b.Property<int>("FinishedAction")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Hash")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("HostDownloadAction")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsFile")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Lifetime")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("Priority")
                        .HasColumnType("INTEGER");

                    b.Property<DateTimeOffset?>("RdAdded")
                        .HasColumnType("TEXT");

                    b.Property<DateTimeOffset?>("RdEnded")
                        .HasColumnType("TEXT");

                    b.Property<string>("RdFiles")
                        .HasColumnType("TEXT");

                    b.Property<string>("RdHost")
                        .HasColumnType("TEXT");

                    b.Property<string>("RdId")
                        .HasColumnType("TEXT");

                    b.Property<string>("RdName")
                        .HasColumnType("TEXT");

                    b.Property<long?>("RdProgress")
                        .HasColumnType("INTEGER");

                    b.Property<long?>("RdSeeders")
                        .HasColumnType("INTEGER");

                    b.Property<long?>("RdSize")
                        .HasColumnType("INTEGER");

                    b.Property<long?>("RdSpeed")
                        .HasColumnType("INTEGER");

                    b.Property<long?>("RdSplit")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("RdStatus")
                        .HasColumnType("INTEGER");

                    b.Property<string>("RdStatusRaw")
                        .HasColumnType("TEXT");

                    b.Property<DateTimeOffset?>("Retry")
                        .HasColumnType("TEXT");

                    b.Property<int>("RetryCount")
                        .HasColumnType("INTEGER");

                    b.Property<int>("TorrentRetryAttempts")
                        .HasColumnType("INTEGER");

                    b.HasKey("TorrentId");

                    b.ToTable("Torrents");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>", b =>
                {
                    b.HasOne("Microsoft.AspNetCore.Identity.IdentityRole", null)
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<string>", b =>
                {
                    b.HasOne("Microsoft.AspNetCore.Identity.IdentityUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<string>", b =>
                {
                    b.HasOne("Microsoft.AspNetCore.Identity.IdentityUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<string>", b =>
                {
                    b.HasOne("Microsoft.AspNetCore.Identity.IdentityRole", null)
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("Microsoft.AspNetCore.Identity.IdentityUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<string>", b =>
                {
                    b.HasOne("Microsoft.AspNetCore.Identity.IdentityUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();
                });

            modelBuilder.Entity("RdtClient.Data.Models.Data.Download", b =>
                {
                    b.HasOne("RdtClient.Data.Models.Data.Torrent", "Torrent")
                        .WithMany("Downloads")
                        .HasForeignKey("TorrentId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Torrent");
                });

            modelBuilder.Entity("RdtClient.Data.Models.Data.Torrent", b =>
                {
                    b.Navigation("Downloads");
                });
#pragma warning restore 612, 618
        }
    }
}
