﻿// <auto-generated />
using System;
using LamaBot.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace LamaBot.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    partial class ApplicationDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.3");

            modelBuilder.Entity("LamaBot.Database.DbCronMessage", b =>
                {
                    b.Property<ulong>("GuildId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Id")
                        .HasColumnType("TEXT");

                    b.Property<ulong>("ChannelId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Content")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Schedule")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("GuildId", "Id");

                    b.ToTable("CronMessages");
                });

            modelBuilder.Entity("LamaBot.Database.DbQuote", b =>
                {
                    b.Property<ulong>("GuildId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Id")
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("ChannelId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("ChannelName")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Content")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<ulong>("MessageId")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("TimestampUtc")
                        .HasColumnType("TEXT");

                    b.Property<ulong>("UserId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("UserName")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("GuildId", "Id");

                    b.HasIndex("UserName");

                    b.ToTable("Quotes");
                });

            modelBuilder.Entity("LamaBot.Database.DbServerSetting", b =>
                {
                    b.Property<ulong>("GuildId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Code")
                        .HasColumnType("TEXT");

                    b.Property<string>("Value")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("GuildId", "Code");

                    b.ToTable("ServerSettings");
                });
#pragma warning restore 612, 618
        }
    }
}
