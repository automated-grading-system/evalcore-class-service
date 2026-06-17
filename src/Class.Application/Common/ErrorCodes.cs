namespace Class.Application.Common;

public static class ErrorCodes
{
    public const string ValidationError = "VALIDATION_ERROR";
    public const string Unauthorized = "UNAUTHORIZED";
    public const string Forbidden = "FORBIDDEN";
    public const string NotFound = "NOT_FOUND";
    public const string Conflict = "CONFLICT";
    public const string InternalError = "INTERNAL_ERROR";

    public const string ClassNotFound = "CLASS_NOT_FOUND";
    public const string ClassAccessDenied = "CLASS_ACCESS_DENIED";
    public const string AlreadyJoinedClass = "ALREADY_JOINED_CLASS";
    public const string StudentNotInClass = "STUDENT_NOT_IN_CLASS";

    public const string LabNotFound = "LAB_NOT_FOUND";
    public const string LabAccessDenied = "LAB_ACCESS_DENIED";
    public const string LabDeadlineInvalid = "LAB_DEADLINE_INVALID";
    public const string LabAssetInvalidFileType = "LAB_ASSET_INVALID_FILE_TYPE";
    public const string LabAssetsAlreadyCompleted = "LAB_ASSETS_ALREADY_COMPLETED";
    public const string LabAssetsNotCompleted = "LAB_ASSETS_NOT_COMPLETED";
    public const string LabNotActive = "LAB_NOT_ACTIVE";
    public const string S3PresignFailed = "S3_PRESIGN_FAILED";
    public const string S3ObjectNotFound = "S3_OBJECT_NOT_FOUND";
    public const string S3ObjectCheckFailed = "S3_OBJECT_CHECK_FAILED";

    public const string OutboxPublishFailed = "OUTBOX_PUBLISH_FAILED";
}
