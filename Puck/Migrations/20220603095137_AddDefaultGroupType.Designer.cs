﻿// <auto-generated />
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Puck.Databases;

#nullable disable

namespace Puck.Migrations
{
    [DbContext(typeof(GuildConfigDatabase))]
    [Migration("20220603095137_AddDefaultGroupType")]
    partial class AddDefaultGroupType
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "6.0.5");

            modelBuilder.Entity("Puck.Database.GroupType", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("GroupTypeName")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("GuildId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("GuildId");

                    b.ToTable("GroupTypes");
                });

            modelBuilder.Entity("Puck.Database.GuildConfig", b =>
                {
                    b.Property<string>("GuildId")
                        .HasColumnType("TEXT");

                    b.Property<double>("DefaultDurationMsec")
                        .HasColumnType("REAL");

                    b.Property<string>("DefaultGroupType")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("GuildName")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("GuildId");

                    b.ToTable("GuildConfigs");
                });

            modelBuilder.Entity("Puck.Database.GuildRole", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("GroupTypeName")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("GuildId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("RoleId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("GuildId");

                    b.ToTable("GuildRoles");
                });

            modelBuilder.Entity("Puck.Database.GroupType", b =>
                {
                    b.HasOne("Puck.Database.GuildConfig", null)
                        .WithMany("AllowedGroupTypes")
                        .HasForeignKey("GuildId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Puck.Database.GuildRole", b =>
                {
                    b.HasOne("Puck.Database.GuildConfig", null)
                        .WithMany("AllowedRoles")
                        .HasForeignKey("GuildId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Puck.Database.GuildConfig", b =>
                {
                    b.Navigation("AllowedGroupTypes");

                    b.Navigation("AllowedRoles");
                });
#pragma warning restore 612, 618
        }
    }
}
