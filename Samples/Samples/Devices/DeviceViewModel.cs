﻿using System;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Windows.Input;
using Plugin.BluetoothLE;
using ReactiveUI;
using Samples.Infrastructure;


namespace Samples.Devices
{
    public class DeviceViewModel : ViewModel
    {
        IDevice device;


        public DeviceViewModel()
        {
            this.SelectCharacteristic = ReactiveCommand.Create<GattCharacteristicViewModel>(x => x.Select());

            this.ConnectionToggle = ReactiveCommand.CreateFromTask(async x =>
            {
                try
                {
                    // don't cleanup connection - force user to d/c
                    if (this.device.Status == ConnectionStatus.Disconnected)
                    {
                        this.device.Connect();
                    }
                    else
                    {
                        this.device.CancelConnection();
                    }
                }
                catch (Exception ex)
                {
                    //this.Dialogs.Alert(ex.ToString());
                }
            });
            this.PairToDevice = ReactiveCommand.CreateFromTask(async x =>
            {
                if (!this.device.Features.HasFlag(DeviceFeatures.PairingRequests))
                {
                    //this.Dialogs.Alert("Pairing is not supported on this platform");
                }
                else if (this.device.PairingStatus == PairingStatus.Paired)
                {
                    //this.Dialogs.Alert("Device is already paired");
                }
                else
                {
                    await this.device.PairingRequest();
                }
            });
            this.RequestMtu = ReactiveCommand.CreateFromTask(
                async x =>
                {
                    if (!this.device.Features.HasFlag(DeviceFeatures.MtuRequests))
                    {
                        //this.Dialogs.Alert("MTU Request not supported on this platform");
                    }
                    else
                    {
                        //var result = await this.Dialogs.PromptAsync(new PromptConfig()
                        //    .SetTitle("MTU Request")
                        //    .SetMessage("Range 20-512")
                        //    .SetInputMode(InputType.Number)
                        //    .SetOnTextChanged(args =>
                        //    {
                        //        var len = args.Value?.Length ?? 0;
                        //        if (len > 0)
                        //        {
                        //            if (len > 3)
                        //            {
                        //                args.Value = args.Value.Substring(0, 3);
                        //            }
                        //            else
                        //            {
                        //                var value = Int32.Parse(args.Value);
                        //                args.IsValid = value >= 20 && value <= 512;
                        //            }
                        //        }
                        //    })
                        //);
                        //if (result.Ok)
                        //{
                        //    var actual = await this.device.RequestMtu(Int32.Parse(result.Text));
                        //    this.Dialogs.Alert("MTU Changed to " + actual);
                        //}
                    }
                },
                this.WhenAny(
                    x => x.ConnectText,
                    x => x.GetValue().Equals("Disconnect")
                )
            );
        }


        public override void OnActivated()
        {
            base.OnActivated();

            this.device
                .WhenStatusChanged()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(status =>
                {
                    switch (status)
                    {
                        case ConnectionStatus.Connecting:
                            this.ConnectText = "Cancel Connection";
                            break;

                        case ConnectionStatus.Connected:
                            this.ConnectText = "Disconnect";
                            break;

                        case ConnectionStatus.Disconnected:
                            this.ConnectText = "Connect";
                            this.GattCharacteristics.Clear();
                            break;
                    }
                });

            this.device
                .WhenAnyCharacteristicDiscovered()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(chs =>
                {

                });
        }


        public ICommand ConnectionToggle { get; }
        public ICommand PairToDevice { get; }
        public ICommand RequestMtu { get; }
        public ICommand SelectCharacteristic { get; }


        public string Name => this.device.Name ?? "Unknown";
        public Guid Uuid => this.device.Uuid;
        public PairingStatus PairingStatus => this.device.PairingStatus;
        public ObservableCollection<Group<GattCharacteristicViewModel>> GattCharacteristics { get; } = new ObservableCollection<Group<GattCharacteristicViewModel>>();


        string connectText = "Connect";
        public string ConnectText
        {
            get => this.connectText;
            private set => this.RaiseAndSetIfChanged(ref this.connectText, value);
        }
    }
}