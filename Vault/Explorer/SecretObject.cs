﻿using Microsoft.Azure.KeyVault;
using Microsoft.PS.Common.Types;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing.Design;
using System.Text;

namespace Microsoft.PS.Common.Vault.Explorer
{
    [DefaultProperty("Name")]
    public class SecretObject : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string info) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));

        [Browsable(false)]
        public string Id { get; }

        [DisplayName("Name")]
        [Browsable(false)]
        public string Name { get; set; }

        private ObservableTagItemsCollection _tags;
        [DisplayName("Custom Tags")]
        public ObservableTagItemsCollection Tags
        {
            get
            {
                return _tags;
            }
            set
            {
                _tags = value;
                NotifyPropertyChanged(nameof(Tags));
            }
        }

        private bool? _enabled;
        [DisplayName("Enabled")]
        public bool? Enabled
        {
            get
            {
                return _enabled;
            }
            set
            {
                _enabled = value;
                NotifyPropertyChanged(nameof(Enabled));
            }
        }

        private DateTime? _expires;
        [DisplayName("Valid until time (UTC)")]
        [Editor(typeof(NullableDateTimePickerEditor), typeof(UITypeEditor))]
        public DateTime? Expires
        {
            get
            {
                return _expires;
            }
            set
            {
                _expires = value;
                NotifyPropertyChanged(nameof(Expires));
            }
        }

        private DateTime? _notBefore;
        [DisplayName("Valid from time (UTC)")]
        [Editor(typeof(NullableDateTimePickerEditor), typeof(UITypeEditor))]
        public DateTime? NotBefore
        {
            get
            {
                return _notBefore;
            }
            set
            {
                _notBefore = value;
                NotifyPropertyChanged(nameof(NotBefore));
            }
        }

        private ContentType _contentType;
        [DisplayName("Content Type")]
        [TypeConverter(typeof(ContentTypeEnumConverter))]
        public ContentType ContentType
        {
            get
            {
                return _contentType;
            }
            set
            {
                _contentType = value;
                NotifyPropertyChanged(nameof(ContentType));
            }
        }

        /// <summary>
        /// Human readable value of the secret
        /// </summary>
        private string _value;
        [DisplayName("Value")]
        [Editor(typeof(MultilineStringEditor), typeof(UITypeEditor))]
        [Browsable(false)]
        public string Value
        {
            get
            {
                return _value;
            }
            set
            {
                if (_value != value)
                {
                    _value = value;
                    NotifyPropertyChanged(nameof(Value));
                }
            }
        }

        /// <summary>
        /// Raw value to store in the vault
        /// </summary>
        [Browsable(false)]
        public string RawValue => ContentType.ToRawValue(_value);

        /// <summary>
        /// Md5 of the raw value
        /// </summary>
        [Browsable(false)]
        public string Md5 => Utils.CalculateMd5(RawValue);

        public SecretObject(Secret secret, PropertyChangedEventHandler propertyChanged)
        {
            Id = secret.Id;
            Name = secret.SecretIdentifier?.Name;
            // get and set
            _tags = new ObservableTagItemsCollection();
            if (null != secret.Tags)
            {
                foreach (var kvp in secret.Tags)
                {
                    _tags.Add(new TagItem(kvp));
                }
            }
            _tags.SetPropertyChangedEventHandler(propertyChanged);
            _enabled = secret.Attributes.Enabled;
            _expires = secret.Attributes.Expires;
            _notBefore = secret.Attributes.NotBefore;
            _contentType = ContentTypeEnumConverter.GetValue(secret.ContentType);
            _value = _contentType.FromRawValue(secret.Value);

            PropertyChanged += propertyChanged;
        }

        public Dictionary<string, string> TagsToDictionary()
        {
            var result = new Dictionary<string, string>();
            foreach (var tagItem in _tags)
            {
                result.Add(tagItem.Name, tagItem.Value);
            }
            result[Consts.Md5Key] = Md5;
            return Utils.AddChangedBy(result);
        }

        public SecretAttributes ToSecretAttributes() => new SecretAttributes()
        {
            Enabled = _enabled,
            Expires = _expires,
            NotBefore = _notBefore
        };

        public string GetFileName() => Name + ContentType.ToExtension();

        public string GetClipboardValue()
        {
            return ContentType.IsCertificate() ? CertificateValueObject.FromValue(Value).Password : Value;
        }

        public byte[] GetSaveToFileValue()
        {
            return ContentType.IsCertificate() ? Convert.FromBase64String(CertificateValueObject.FromValue(Value).Data) : Encoding.UTF8.GetBytes(Value);
        }

        public DataObjectEx.SelectedItem GetSaveToFileDataObject()
        {
            byte[] contents = GetSaveToFileValue();
            return new DataObjectEx.SelectedItem()
            {
                FileName = GetFileName(),
                FileContents = contents,
                FileSize = contents.Length,
                WriteTime = DateTime.Now,
            };
        }
    }
}
