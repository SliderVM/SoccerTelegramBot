﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SoccerTelegramBot;

#nullable disable

namespace SoccerTelegramBot.Migrations
{
    [DbContext(typeof(DatabaseContext))]
    partial class DatabaseContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "7.0.5");

            modelBuilder.Entity("SoccerTelegramBot.Entities.Configuration", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Label")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.Property<string>("Value")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Configurations");

                    b.HasData(
                        new
                        {
                            Id = 1,
                            Label = "gameday",
                            Name = "День игры",
                            Value = "3"
                        },
                        new
                        {
                            Id = 2,
                            Label = "costonegame",
                            Name = "Стоимость одной игрый",
                            Value = "250"
                        },
                        new
                        {
                            Id = 3,
                            Label = "costsubscribe",
                            Name = "Стоимоить абонимента",
                            Value = "1000"
                        },
                        new
                        {
                            Id = 4,
                            Label = "gametime",
                            Name = "Время игры",
                            Value = "19:50"
                        },
                        new
                        {
                            Id = 5,
                            Label = "su",
                            Name = "Суперпользователь",
                            Value = "217340949"
                        });
                });

            modelBuilder.Entity("SoccerTelegramBot.Entities.Signed", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("GameDate")
                        .HasColumnType("TEXT");

                    b.Property<bool>("IsPayment")
                        .HasColumnType("INTEGER");

                    b.Property<long?>("UserId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("Signeds");
                });

            modelBuilder.Entity("SoccerTelegramBot.Entities.Subscription", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsActive")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER")
                        .HasDefaultValue(false);

                    b.Property<int>("Month")
                        .HasColumnType("INTEGER");

                    b.Property<long?>("UserId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Year")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("Subscriptions");
                });

            modelBuilder.Entity("SoccerTelegramBot.Entities.User", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("FirstName")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<bool>("IsAdmin")
                        .HasColumnType("INTEGER");

                    b.Property<string>("LastName")
                        .HasColumnType("TEXT");

                    b.Property<string>("UserName")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("SoccerTelegramBot.Entities.Signed", b =>
                {
                    b.HasOne("SoccerTelegramBot.Entities.User", "User")
                        .WithMany("Signeds")
                        .HasForeignKey("UserId");

                    b.Navigation("User");
                });

            modelBuilder.Entity("SoccerTelegramBot.Entities.Subscription", b =>
                {
                    b.HasOne("SoccerTelegramBot.Entities.User", "User")
                        .WithMany("Subscriptions")
                        .HasForeignKey("UserId");

                    b.Navigation("User");
                });

            modelBuilder.Entity("SoccerTelegramBot.Entities.User", b =>
                {
                    b.Navigation("Signeds");

                    b.Navigation("Subscriptions");
                });
#pragma warning restore 612, 618
        }
    }
}