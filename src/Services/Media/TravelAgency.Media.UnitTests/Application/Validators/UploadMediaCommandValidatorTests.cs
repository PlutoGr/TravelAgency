using FluentValidation.TestHelper;
using Microsoft.Extensions.Options;
using TravelAgency.Media.Application.Features.Upload;
using TravelAgency.Media.Application.Settings;

namespace TravelAgency.Media.UnitTests.Application.Validators;

public class UploadMediaCommandValidatorTests
{
    private readonly UploadMediaCommandValidator _validator;

    public UploadMediaCommandValidatorTests()
    {
        var settings = new UploadSettings
        {
            MaxFileSizeBytes = 10 * 1024 * 1024,
            AllowedMimeTypes = ["image/jpeg", "image/png", "image/webp", "image/gif", "application/pdf"],
            ThumbnailWidths = [200, 800]
        };

        _validator = new UploadMediaCommandValidator(Options.Create(settings));
    }

    private static UploadMediaCommand ValidCommand() =>
        new(CreateValidJpegStream(), "photo.jpg", "image/jpeg", 1024);

    private static MemoryStream CreateValidJpegStream()
    {
        var ms = new MemoryStream();
        ms.Write([0xFF, 0xD8, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00]);
        ms.Position = 0;
        return ms;
    }

    [Fact]
    public async Task Validate_ValidCommand_PassesValidation()
    {
        var result = await _validator.TestValidateAsync(ValidCommand());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_EmptyFileName_FailsWithRequiredMessage()
    {
        var command = ValidCommand() with { FileName = "" };

        var result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(x => x.FileName)
            .WithErrorMessage("File name is required.");
    }

    [Fact]
    public async Task Validate_WhitespaceFileName_FailsValidation()
    {
        var command = ValidCommand() with { FileName = "   " };

        var result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(x => x.FileName);
    }

    [Theory]
    [InlineData("application/octet-stream")]
    [InlineData("text/plain")]
    [InlineData("video/mp4")]
    [InlineData("image/bmp")]
    public async Task Validate_DisallowedContentType_FailsValidation(string contentType)
    {
        var command = ValidCommand() with { ContentType = contentType };

        var result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(x => x.ContentType);
    }

    [Theory]
    [InlineData("image/jpeg", new byte[] { 0xFF, 0xD8, 0xFF })]
    [InlineData("image/png", new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A })]
    [InlineData("image/gif", new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 })]
    [InlineData("application/pdf", new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D })]
    public async Task Validate_AllowedContentTypesWithMatchingMagicBytes_PassValidation(string contentType, byte[] magicBytes)
    {
        var ms = new MemoryStream();
        ms.Write(magicBytes);
        ms.Position = 0;
        var command = ValidCommand() with { ContentType = contentType, FileContent = ms };

        var result = await _validator.TestValidateAsync(command);

        result.ShouldNotHaveValidationErrorFor(x => x.ContentType);
    }

    [Fact]
    public async Task Validate_ImageWebpWithMatchingMagicBytes_PassValidation()
    {
        var ms = new MemoryStream();
        ms.Write([0x52, 0x49, 0x46, 0x46, 0x00, 0x00, 0x00, 0x00, 0x57, 0x45, 0x42, 0x50]);
        ms.Position = 0;
        var command = ValidCommand() with { ContentType = "image/webp", FileContent = ms };

        var result = await _validator.TestValidateAsync(command);

        result.ShouldNotHaveValidationErrorFor(x => x.ContentType);
    }

    [Fact]
    public async Task Validate_ContentTypeMismatchMagicBytes_FailsValidation()
    {
        var command = ValidCommand() with { ContentType = "image/png" }; // JPEG magic bytes in stream

        var result = await _validator.TestValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "File content does not match declared content type.");
    }

    [Fact]
    public async Task Validate_SizeBytesZero_FailsWithEmptyFileMessage()
    {
        var command = ValidCommand() with { SizeBytes = 0 };

        var result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(x => x.SizeBytes)
            .WithErrorMessage("File must not be empty.");
    }

    [Fact]
    public async Task Validate_NegativeSizeBytes_FailsValidation()
    {
        var command = ValidCommand() with { SizeBytes = -1 };

        var result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(x => x.SizeBytes);
    }

    [Fact]
    public async Task Validate_SizeBytesOverLimit_FailsValidation()
    {
        var command = ValidCommand() with { SizeBytes = 10 * 1024 * 1024 + 1 };

        var result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(x => x.SizeBytes);
    }

    [Fact]
    public async Task Validate_SizeBytesAtExactLimit_PassesValidation()
    {
        var command = ValidCommand() with { SizeBytes = 10 * 1024 * 1024 };

        var result = await _validator.TestValidateAsync(command);

        result.ShouldNotHaveValidationErrorFor(x => x.SizeBytes);
    }

    [Fact]
    public async Task Validate_EmptyContentType_FailsValidation()
    {
        var command = ValidCommand() with { ContentType = "" };

        var result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(x => x.ContentType);
    }

    [Fact]
    public async Task Validate_NonSeekableStream_FailsValidation()
    {
        var nonSeekable = new NonSeekableStream(CreateValidJpegStream());
        var command = ValidCommand() with { FileContent = nonSeekable };

        var result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(x => x.FileContent)
            .WithErrorMessage("File stream must be seekable for validation.");
    }

    private sealed class NonSeekableStream : Stream
    {
        private readonly Stream _inner;

        public NonSeekableStream(Stream inner) => _inner = inner;

        public override bool CanSeek => false;
        public override bool CanRead => _inner.CanRead;
        public override bool CanWrite => false;
        public override long Length => _inner.Length;
        public override long Position { get => _inner.Position; set => throw new NotSupportedException(); }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override void Flush() { }
        protected override void Dispose(bool disposing) => _inner.Dispose();
    }
}
