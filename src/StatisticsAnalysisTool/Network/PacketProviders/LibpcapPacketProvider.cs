﻿#nullable enable

using BinaryFormat;
using BinaryFormat.EthernetFrame;
using BinaryFormat.IPv4;
using BinaryFormat.Udp;
using Libpcap;
using Serilog;
using StatisticsAnalysisTool.Common.UserSettings;
using System;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;

namespace StatisticsAnalysisTool.Network.PacketProviders;

public class LibpcapPacketProvider : PacketProvider
{
    private readonly IPhotonReceiver _photonReceiver;
    private readonly PcapDispatcher _dispatcher;
    private CancellationTokenSource? _cts;
    private Thread? _thread;

    public override bool IsRunning => _thread is { IsAlive: true };

    public LibpcapPacketProvider(IPhotonReceiver photonReceiver)
    {
        _photonReceiver = photonReceiver ?? throw new ArgumentNullException(nameof(photonReceiver));

        _dispatcher = new PcapDispatcher(Dispatch);
        _thread = new Thread(Worker)
        {
            IsBackground = true
        };
    }

    public override void Start()
    {
        var devices = Pcap.ListDevices();

        int deviceId = 0;
        foreach (var device in devices)
        {
            if (SettingsController.CurrentSettings.NetworkDevice > 0 && SettingsController.CurrentSettings.NetworkDevice != deviceId)
            {
                Log.Information("NetworkManager (npcap)[ID:{deviceId}]: manually skipping device {Device}:{DeviceDescription}",
                    deviceId++, device.Name, device.Description);
                continue;
            }

            if (device.Type != NetworkInterfaceType.Ethernet && device.Type != NetworkInterfaceType.Wireless80211)
            {
                Log.Information("NetworkManager (npcap)[ID:{deviceId}]: skipping device {Device}:{DeviceDescription} due to unsupported type {Devicetype}",
                    deviceId++, device.Name, device.Description, device.Type);
                continue;
            }
            if (device.Flags.HasFlag(PcapDeviceFlags.Loopback))
            {
                Log.Information("NetworkManager (npcap)[ID:{deviceId}]: skipping device {Device}:{DeviceDescription} due to loopback flag",
                    deviceId++, device.Name, device.Description);
                continue;
            }
            if (!device.Flags.HasFlag(PcapDeviceFlags.Up))
            {
                Log.Information("NetworkManager (npcap)[ID:{deviceId}]: skipping device {Device}:{DeviceDescription} due not being up",
                    deviceId++, device.Name, device.Description);
                continue;
            }

            Log.Information("NetworkManager (npcap)[ID:{deviceId}]: opening device {Device}:{DeviceDescription}",
                deviceId++, device.Name, device.Description);
            _dispatcher.OpenDevice(device, pcap =>
            {
                pcap.NonBlocking = true;
            });

            _dispatcher.Filter = SettingsController.CurrentSettings.PacketFilter;
        }

        _cts = new CancellationTokenSource();
        _thread = new Thread(Worker)
        {
            IsBackground = true
        };

        _thread.Start();
    }

    private void Dispatch(Pcap pcap, ref Packet packet)
    {
        var ethernetFrameReader = new BinaryFormatReader(packet.Data);
        var ethernetFrame = new L2EthernetFrameShape();
        if (!ethernetFrameReader.TryReadL2EthernetFrame(ref ethernetFrame))
        {
            return;
        }

        var ipv4PacketReader = new BinaryFormatReader(ethernetFrame.Payload);
        var ipv4Packet = new IPv4PacketShape();
        if (!ipv4PacketReader.TryReadIPv4Packet(ref ipv4Packet))
        {
            return;
        }

        if (ipv4Packet.Protocol != (byte) ProtocolType.Udp)
        {
            return;
        }

        var udpPacketReader = new BinaryFormatReader(ipv4Packet.Payload);
        var udpPacket = new UdpPacketShape();
        if (!udpPacketReader.TryReadUdpPacket(ref udpPacket))
        {
            return;
        }

        if (udpPacket.SourcePort != 5056 && udpPacket.DestinationPort != 5056)
        {
            return;
        }

        try
        {
            _photonReceiver.ReceivePacket(udpPacket.Payload.ToArray());
        }
        catch
        {
            // ignored
        }
    }

    private void Worker()
    {
        try
        {
            while (_cts is { IsCancellationRequested: false })
            {
                try
                {
                    var dispatched = _dispatcher.Dispatch(50);
                    if (dispatched <= 0)
                    {
                        Thread.Sleep(25);
                    }
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (InvalidOperationException)
                {
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.Print("Worker Exception: " + ex.Message);
        }
    }

    public override void Stop()
    {
        _dispatcher.Dispose();

        _cts?.Cancel();
        _thread?.Join();

        _cts?.Dispose();
        _cts = null;
        _thread = null;
    }
}