﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PdfSorter.Data;

#nullable disable

namespace PdfSorter.Migrations
{
    [DbContext(typeof(ProcessingMetadataContext))]
    [Migration("20230227012402_Init")]
    partial class Init
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "7.0.3");

            modelBuilder.Entity("PdfSorter.Data.ProcessEvent", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("CompleteTime")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("StartTime")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("ProcessEvents");
                });

            modelBuilder.Entity("PdfSorter.Data.ProcessedFile", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("FileName")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("PONumber")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("ProcessedDateTime")
                        .HasColumnType("TEXT");

                    b.Property<Guid>("ProcessedZipId")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("ProcessedZipId");

                    b.ToTable("ProcessedFiles");
                });

            modelBuilder.Entity("PdfSorter.Data.ProcessedZip", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("FileName")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("LastUpdateDateTime")
                        .HasColumnType("TEXT");

                    b.Property<Guid>("ProcessEventId")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("ProcessEventId");

                    b.ToTable("ProcessedZips");
                });

            modelBuilder.Entity("PdfSorter.Data.ProcessedFile", b =>
                {
                    b.HasOne("PdfSorter.Data.ProcessedZip", "ProcessedZip")
                        .WithMany("ProcessedFiles")
                        .HasForeignKey("ProcessedZipId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("ProcessedZip");
                });

            modelBuilder.Entity("PdfSorter.Data.ProcessedZip", b =>
                {
                    b.HasOne("PdfSorter.Data.ProcessEvent", "ProcessEvent")
                        .WithMany("ProcessedZips")
                        .HasForeignKey("ProcessEventId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("ProcessEvent");
                });

            modelBuilder.Entity("PdfSorter.Data.ProcessEvent", b =>
                {
                    b.Navigation("ProcessedZips");
                });

            modelBuilder.Entity("PdfSorter.Data.ProcessedZip", b =>
                {
                    b.Navigation("ProcessedFiles");
                });
#pragma warning restore 612, 618
        }
    }
}