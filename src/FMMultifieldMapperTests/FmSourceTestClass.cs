﻿using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using FMMultiFieldMapper;
using FMMultiFieldMapper.Sync;

namespace FMMultifieldMapperTests;

[DataContract(Name = "TestLayout")]
public class FmSourceTestClass : IFmObject
{
    [NotMapped]
    public int FileMakerRecordId { get; set; }
    [DataMember(Name = "Themen(1)")]
    [FileMakerMultiField(MultiFieldName = "Themen", Order = 0)]
    public string? Themen1 { get; set; }
    [DataMember(Name = "Themen(2)")]
    [FileMakerMultiField(MultiFieldName = "Themen", Order = 1)]
    public string? Themen2 { get; set; }
    [DataMember(Name = "Themen(3)")]
    [FileMakerMultiField(MultiFieldName = "Themen", Order = 2)]
    public string? Themen3 { get; set; }
    [DataMember(Name = "Name")]
    public string? Name { get; set; }
    [DataMember(Name = "AutoAndDatum")]
    public string? ModificationDate { get; set; }
}

[DataContract(Name = "TestLayout")]
public class FmSourceTestClassSync : IFmSync
{
    [NotMapped]
    public int FileMakerRecordId { get; set; }
    [DataMember(Name = "AutoAndDatum")]
    public string? ModificationDate { get; set; }
}
