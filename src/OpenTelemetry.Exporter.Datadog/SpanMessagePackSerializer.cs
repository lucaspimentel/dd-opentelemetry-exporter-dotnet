using System;
using MsgPack;
using MsgPack.Serialization;

namespace OpenTelemetry.Exporter.Datadog
{
    internal class SpanMessagePackSerializer : MessagePackSerializer<SpanModel>
    {
        public SpanMessagePackSerializer(SerializationContext context)
            : base(context)
        {
        }

        protected override void PackToCore(Packer packer, SpanModel value)
        {
            // First, pack array length (or map length).
            // It should be the number of members of the object to be serialized.
            var len = 8;

            if (value.ParentId != 0)
            {
                len++;
            }

            if (value.Error)
            {
                len++;
            }

            if (value.Tags != null)
            {
                len++;
            }

            if (value.Metrics != null)
            {
                len++;
            }

            packer.PackMapHeader(len);
            packer.PackString("trace_id");
            packer.Pack(value.TraceId);

            packer.PackString("span_id");
            packer.Pack(value.SpanId);

            packer.PackString("name");
            packer.PackString(value.OperationName);

            packer.PackString("resource");
            packer.PackString(value.ResourceName);

            packer.PackString("service");
            packer.PackString(value.ServiceName);

            packer.PackString("type");
            packer.PackString(value.Type);

            packer.PackString("start");
            packer.Pack(Util.ToUnixTimeNanoseconds(value.StartTime));

            packer.PackString("duration");
            packer.Pack(Util.ToNanoseconds(value.Duration));

            if (value.ParentId != 0)
            {
                packer.PackString("parent_id");
                packer.Pack(value.ParentId);
            }

            if (value.Error)
            {
                packer.PackString("error");
                packer.Pack(1);
            }

            if (value.Tags != null)
            {
                packer.PackString("meta");
                packer.Pack(value.Tags);
            }

            if (value.Metrics != null)
            {
                packer.PackString("metrics");
                packer.PackDictionary(value.Metrics);
            }
        }

        protected override SpanModel UnpackFromCore(Unpacker unpacker)
        {
            throw new NotImplementedException();
        }
    }
}
