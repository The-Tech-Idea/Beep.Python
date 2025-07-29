using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Beep.Python.Model
{
    #region Security and Compliance

    /// <summary>
    /// Security and compliance framework for transformer pipelines
    /// </summary>
    public interface ITransformerSecurity
    {
        Task<SecurityAssessment> AssessSecurityAsync(string pipelineId);
        Task<bool> ApplySecurityPolicyAsync(string pipelineId, SecurityPolicy policy);
        Task<ComplianceReport> GenerateComplianceReportAsync(ComplianceFramework framework);
        Task<bool> ValidateDataPrivacyAsync(string dataPath, PrivacyRequirements requirements);
        Task<AuditTrail> GetAuditTrailAsync(string pipelineId, DateTime startDate, DateTime endDate);
        Task LogSecurityEventAsync(SecurityEvent securityEvent);
        Task<List<SecurityVulnerability>> ScanForVulnerabilitiesAsync(string pipelineId);
        Task<bool> EncryptModelDataAsync(string modelId, EncryptionConfig config);
    }

    /// <summary>
    /// Security assessment results
    /// </summary>
    public class SecurityAssessment
    {
        public string PipelineId { get; set; } = string.Empty;
        public SecurityRating OverallRating { get; set; }
        public List<SecurityFinding> Findings { get; set; } = new();
        public List<SecurityRecommendation> Recommendations { get; set; } = new();
        public DateTime AssessmentDate { get; set; }
        public string AssessedBy { get; set; } = string.Empty;
        public Dictionary<SecurityDomain, SecurityRating> DomainRatings { get; set; } = new();
    }

    /// <summary>
    /// Security finding details
    /// </summary>
    public class SecurityFinding
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public SecurityRating Severity { get; set; }
        public SecurityDomain Domain { get; set; }
        public string AffectedComponent { get; set; } = string.Empty;
        public DateTime DiscoveredDate { get; set; }
        public string Evidence { get; set; } = string.Empty;
        public List<string> Remediation { get; set; } = new();
        public bool IsResolved { get; set; }
    }

    /// <summary>
    /// Security recommendation
    /// </summary>
    public class SecurityRecommendation
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public SecurityRating Priority { get; set; }
        public SecurityDomain Domain { get; set; }
        public List<string> ActionItems { get; set; } = new();
        public TimeSpan EstimatedEffort { get; set; }
        public string BusinessJustification { get; set; } = string.Empty;
        public bool IsImplemented { get; set; }
    }

    /// <summary>
    /// Security policy configuration
    /// </summary>
    public class SecurityPolicy
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public DataClassificationLevel DataClassification { get; set; }
        public AccessControlPolicy AccessControl { get; set; } = new();
        public EncryptionRequirements Encryption { get; set; } = new();
        public AuditingRequirements Auditing { get; set; } = new();
        public DataRetentionPolicy DataRetention { get; set; } = new();
        public List<ComplianceFramework> RequiredCompliance { get; set; } = new();
        public bool RequireModelSigning { get; set; }
        public bool RequireSourceVerification { get; set; }
    }

    /// <summary>
    /// Access control policy
    /// </summary>
    public class AccessControlPolicy
    {
        public List<string> AuthorizedUsers { get; set; } = new();
        public List<string> AuthorizedRoles { get; set; } = new();
        public Dictionary<string, List<string>> ResourcePermissions { get; set; } = new();
        public bool RequireMultiFactorAuth { get; set; }
        public TimeSpan SessionTimeout { get; set; }
        public int MaxConcurrentSessions { get; set; }
        public bool RequireApprovalForChanges { get; set; }
    }

    /// <summary>
    /// Encryption requirements
    /// </summary>
    public class EncryptionRequirements
    {
        public bool EncryptAtRest { get; set; } = true;
        public bool EncryptInTransit { get; set; } = true;
        public string EncryptionAlgorithm { get; set; } = "AES-256";
        public string KeyManagementSystem { get; set; } = string.Empty;
        public TimeSpan KeyRotationPeriod { get; set; }
        public bool RequireHardwareSecurityModule { get; set; }
        public List<string> EncryptedFields { get; set; } = new();
    }

    /// <summary>
    /// Auditing requirements
    /// </summary>
    public class AuditingRequirements
    {
        public bool EnableAuditing { get; set; } = true;
        public List<string> AuditedEvents { get; set; } = new();
        public string AuditLogLocation { get; set; } = string.Empty;
        public TimeSpan AuditRetentionPeriod { get; set; }
        public bool RequireDigitalSignatures { get; set; }
        public bool EnableRealTimeAlerting { get; set; }
        public List<string> AlertRecipients { get; set; } = new();
    }

    /// <summary>
    /// Data retention policy
    /// </summary>
    public class DataRetentionPolicy
    {
        public TimeSpan RetentionPeriod { get; set; }
        public bool AutomaticDeletion { get; set; }
        public List<string> ExemptDataTypes { get; set; } = new();
        public string ArchivalLocation { get; set; } = string.Empty;
        public bool RequireApprovalForDeletion { get; set; }
        public List<string> LegalHoldCategories { get; set; } = new();
    }

    /// <summary>
    /// Compliance report for regulatory frameworks
    /// </summary>
    public class ComplianceReport
    {
        public ComplianceFramework Framework { get; set; }
        public ComplianceStatus Status { get; set; }
        public List<ComplianceControl> Controls { get; set; } = new();
        public List<ComplianceGap> Gaps { get; set; } = new();
        public DateTime ReportDate { get; set; }
        public string ReportedBy { get; set; } = string.Empty;
        public List<Evidence> Evidence { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Compliance control
    /// </summary>
    public class ComplianceControl
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ComplianceFramework Framework { get; set; }
        public ComplianceStatus Status { get; set; }
        public DateTime LastAssessed { get; set; }
        public string AssessedBy { get; set; } = string.Empty;
        public List<string> Requirements { get; set; } = new();
        public List<Evidence> Evidence { get; set; } = new();
    }

    /// <summary>
    /// Compliance gap
    /// </summary>
    public class ComplianceGap
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ComplianceFramework Framework { get; set; }
        public string RequiredControl { get; set; } = string.Empty;
        public string CurrentState { get; set; } = string.Empty;
        public string RequiredState { get; set; } = string.Empty;
        public SecurityRating RiskLevel { get; set; }
        public List<string> RemediationSteps { get; set; } = new();
        public DateTime TargetCompletionDate { get; set; }
        public string Owner { get; set; } = string.Empty;
    }

    /// <summary>
    /// Evidence for compliance
    /// </summary>
    public class Evidence
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public DateTime CollectedDate { get; set; }
        public string CollectedBy { get; set; } = string.Empty;
        public bool IsVerified { get; set; }
        public string VerifiedBy { get; set; } = string.Empty;
        public DateTime VerificationDate { get; set; }
    }

    /// <summary>
    /// Security event
    /// </summary>
    public class SecurityEvent
    {
        public string Id { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public SecurityRating Severity { get; set; }
        public DateTime Timestamp { get; set; }
        public string Source { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public Dictionary<string, object> AdditionalData { get; set; } = new();
        public bool RequiresResponse { get; set; }
        public string ResponseAction { get; set; } = string.Empty;
    }

    /// <summary>
    /// Security vulnerability
    /// </summary>
    public class SecurityVulnerability
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public SecurityRating Severity { get; set; }
        public string CveId { get; set; } = string.Empty;
        public string AffectedComponent { get; set; } = string.Empty;
        public string AffectedVersion { get; set; } = string.Empty;
        public string FixedInVersion { get; set; } = string.Empty;
        public DateTime DiscoveredDate { get; set; }
        public DateTime PublishedDate { get; set; }
        public List<string> References { get; set; } = new();
        public bool IsPatched { get; set; }
        public string PatchStatus { get; set; } = string.Empty;
    }

    /// <summary>
    /// Encryption configuration
    /// </summary>
    public class EncryptionConfig
    {
        public string Algorithm { get; set; } = "AES-256";
        public string KeyId { get; set; } = string.Empty;
        public string KeyManagementSystem { get; set; } = string.Empty;
        public bool RotateKeys { get; set; } = true;
        public TimeSpan KeyRotationInterval { get; set; }
        public Dictionary<string, object> AlgorithmParameters { get; set; } = new();
    }

    /// <summary>
    /// Audit trail information
    /// </summary>
    public class AuditTrail
    {
        public string PipelineId { get; set; } = string.Empty;
        public List<AuditEntry> Entries { get; set; } = new();
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalEntries { get; set; }
        public string GeneratedBy { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }
    }

    /// <summary>
    /// Individual audit entry
    /// </summary>
    public class AuditEntry
    {
        public string Id { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Resource { get; set; } = string.Empty;
        public string Result { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;
        public Dictionary<string, object> AdditionalData { get; set; } = new();
    }

    /// <summary>
    /// Privacy requirements
    /// </summary>
    public class PrivacyRequirements
    {
        public List<ComplianceFramework> ApplicableFrameworks { get; set; } = new();
        public bool RequireExplicitConsent { get; set; }
        public bool RequireDataMinimization { get; set; }
        public bool RequirePurposeLimitation { get; set; }
        public bool RequireDataPortability { get; set; }
        public bool RequireRightToErasure { get; set; }
        public List<string> LawfulBases { get; set; } = new();
        public List<string> SpecialCategories { get; set; } = new();
    }

    public enum SecurityRating
    {
        Critical,
        High,
        Medium,
        Low,
        Excellent
    }

    public enum SecurityDomain
    {
        Authentication,
        Authorization,
        DataProtection,
        NetworkSecurity,
        ModelSecurity,
        Compliance,
        Monitoring
    }

    public enum DataClassificationLevel
    {
        Public,
        Internal,
        Confidential,
        Restricted,
        TopSecret
    }

    public enum ComplianceFramework
    {
        GDPR,
        CCPA,
        HIPAA,
        SOX,
        PCI_DSS,
        ISO27001,
        NIST,
        SOC2,
        FedRAMP
    }

    public enum ComplianceStatus
    {
        Compliant,
        NonCompliant,
        PartiallyCompliant,
        NotAssessed
    }

    #endregion

    #region Data Privacy and Protection

    /// <summary>
    /// Data privacy and protection for transformer pipelines
    /// </summary>
    public interface ITransformerPrivacy
    {
        Task<PrivacyAssessment> AssessPrivacyImpactAsync(string pipelineId);
        Task<bool> ApplyPrivacyControlsAsync(string pipelineId, PrivacyControls controls);
        Task<bool> AnonymizeDataAsync(string dataPath, AnonymizationConfig config);
        Task<bool> ApplyDifferentialPrivacyAsync(string modelId, DifferentialPrivacyConfig config);
        Task<DataLineage> TraceDataLineageAsync(string dataId);
        Task<bool> ProcessDataDeletionRequestAsync(DataDeletionRequest request);
        Task<ConsentManagement> GetConsentStatusAsync(string userId);
        Task<bool> ValidateDataMinimizationAsync(string pipelineId);
    }

    /// <summary>
    /// Privacy assessment results
    /// </summary>
    public class PrivacyAssessment
    {
        public string PipelineId { get; set; } = string.Empty;
        public PrivacyRisk OverallRisk { get; set; }
        public List<PrivacyImpact> Impacts { get; set; } = new();
        public List<PersonalDataCategory> PersonalDataTypes { get; set; } = new();
        public DataProcessingPurpose ProcessingPurpose { get; set; }
        public List<DataTransfer> InternationalTransfers { get; set; } = new();
        public RetentionPeriod DataRetention { get; set; } = new();
        public List<PrivacyControl> RecommendedControls { get; set; } = new();
    }

    /// <summary>
    /// Privacy impact assessment
    /// </summary>
    public class PrivacyImpact
    {
        public string Id { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public PrivacyRisk RiskLevel { get; set; }
        public List<string> AffectedDataSubjects { get; set; } = new();
        public List<PersonalDataCategory> AffectedDataTypes { get; set; } = new();
        public List<string> MitigationMeasures { get; set; } = new();
        public string LikelihoodAssessment { get; set; } = string.Empty;
        public string ImpactAssessment { get; set; } = string.Empty;
    }

    /// <summary>
    /// Personal data category
    /// </summary>
    public class PersonalDataCategory
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsSpecialCategory { get; set; }
        public List<string> LawfulBases { get; set; } = new();
        public PrivacyRisk RiskLevel { get; set; }
        public List<string> ProcessingPurposes { get; set; } = new();
    }

    /// <summary>
    /// Data processing purpose
    /// </summary>
    public class DataProcessingPurpose
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string LawfulBasis { get; set; } = string.Empty;
        public bool RequiresConsent { get; set; }
        public List<string> DataCategories { get; set; } = new();
        public RetentionPeriod RetentionPeriod { get; set; } = new();
    }

    /// <summary>
    /// International data transfer
    /// </summary>
    public class DataTransfer
    {
        public string Id { get; set; } = string.Empty;
        public string SourceCountry { get; set; } = string.Empty;
        public string DestinationCountry { get; set; } = string.Empty;
        public string TransferMechanism { get; set; } = string.Empty;
        public bool HasAdequacyDecision { get; set; }
        public List<string> Safeguards { get; set; } = new();
        public DateTime TransferDate { get; set; }
        public string Purpose { get; set; } = string.Empty;
    }

    /// <summary>
    /// Data retention period
    /// </summary>
    public class RetentionPeriod
    {
        public TimeSpan Duration { get; set; }
        public string Justification { get; set; } = string.Empty;
        public bool IsIndefinite { get; set; }
        public List<string> ExceptionCriteria { get; set; } = new();
        public DateTime ReviewDate { get; set; }
        public string DeletionMethod { get; set; } = string.Empty;
    }

    /// <summary>
    /// Privacy control measure
    /// </summary>
    public class PrivacyControl
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool IsImplemented { get; set; }
        public DateTime ImplementationDate { get; set; }
        public string Owner { get; set; } = string.Empty;
        public List<string> RelatedRisks { get; set; } = new();
    }

    /// <summary>
    /// Privacy controls configuration
    /// </summary>
    public class PrivacyControls
    {
        public List<PrivacyControl> Controls { get; set; } = new();
        public ConsentManagement ConsentManagement { get; set; } = new();
        public AnonymizationConfig AnonymizationConfig { get; set; } = new();
        public DifferentialPrivacyConfig DifferentialPrivacyConfig { get; set; } = new();
        public DataLineage DataLineage { get; set; } = new();
    }

    /// <summary>
    /// Data anonymization configuration
    /// </summary>
    public class AnonymizationConfig
    {
        public List<AnonymizationTechnique> Techniques { get; set; } = new();
        public Dictionary<string, AnonymizationRule> FieldRules { get; set; } = new();
        public int KAnonymity { get; set; } = 5;
        public double LDiversity { get; set; } = 2.0;
        public bool PreserveUtility { get; set; } = true;
        public QualityMetrics QualityRequirements { get; set; } = new();
    }

    /// <summary>
    /// Anonymization rule for specific field
    /// </summary>
    public class AnonymizationRule
    {
        public string FieldName { get; set; } = string.Empty;
        public AnonymizationTechnique Technique { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new();
        public bool PreserveNulls { get; set; } = true;
        public string ReplacementValue { get; set; } = string.Empty;
    }

    /// <summary>
    /// Differential privacy configuration
    /// </summary>
    public class DifferentialPrivacyConfig
    {
        public double Epsilon { get; set; } = 1.0;
        public double Delta { get; set; } = 1e-5;
        public string Mechanism { get; set; } = "Laplace";
        public double Sensitivity { get; set; } = 1.0;
        public bool ClampBounds { get; set; } = true;
        public double LowerBound { get; set; } = 0.0;
        public double UpperBound { get; set; } = 1.0;
    }

    /// <summary>
    /// Data lineage tracking
    /// </summary>
    public class DataLineage
    {
        public string DataId { get; set; } = string.Empty;
        public List<DataSource> Sources { get; set; } = new();
        public List<DataTransformation> Transformations { get; set; } = new();
        public List<DataDestination> Destinations { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdated { get; set; }
        public string Version { get; set; } = string.Empty;
    }

    /// <summary>
    /// Data source information
    /// </summary>
    public class DataSource
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public DateTime AccessDate { get; set; }
        public string AccessedBy { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Data transformation step
    /// </summary>
    public class DataTransformation
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime AppliedAt { get; set; }
        public string AppliedBy { get; set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    /// <summary>
    /// Data destination information
    /// </summary>
    public class DataDestination
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public DateTime StoredAt { get; set; }
        public string StoredBy { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Data deletion request
    /// </summary>
    public class DataDeletionRequest
    {
        public string Id { get; set; } = string.Empty;
        public string RequesterId { get; set; } = string.Empty;
        public DateTime RequestDate { get; set; }
        public List<string> DataCategories { get; set; } = new();
        public string Reason { get; set; } = string.Empty;
        public string LegalBasis { get; set; } = string.Empty;
        public DateTime TargetDeletionDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<string> AffectedSystems { get; set; } = new();
    }

    /// <summary>
    /// Consent management
    /// </summary>
    public class ConsentManagement
    {
        public string UserId { get; set; } = string.Empty;
        public Dictionary<string, ConsentRecord> Consents { get; set; } = new();
        public DateTime LastUpdated { get; set; }
        public string ConsentVersion { get; set; } = string.Empty;
        public List<string> WithdrawnConsents { get; set; } = new();
    }

    /// <summary>
    /// Individual consent record
    /// </summary>
    public class ConsentRecord
    {
        public string Purpose { get; set; } = string.Empty;
        public bool IsGiven { get; set; }
        public DateTime ConsentDate { get; set; }
        public DateTime? WithdrawalDate { get; set; }
        public string ConsentMethod { get; set; } = string.Empty;
        public string Evidence { get; set; } = string.Empty;
        public bool IsExplicit { get; set; }
    }

    /// <summary>
    /// Quality metrics for anonymization
    /// </summary>
    public class QualityMetrics
    {
        public double MinAccuracy { get; set; } = 0.8;
        public double MinPrecision { get; set; } = 0.8;
        public double MinRecall { get; set; } = 0.8;
        public double MaxInformationLoss { get; set; } = 0.2;
        public bool PreserveDistribution { get; set; } = true;
        public List<string> CriticalFields { get; set; } = new();
    }

    public enum PrivacyRisk
    {
        VeryLow,
        Low,
        Medium,
        High,
        VeryHigh
    }

    public enum AnonymizationTechnique
    {
        Generalization,
        Suppression,
        Perturbation,
        Pseudonymization,
        Tokenization,
        Masking,
        Swapping
    }

    #endregion

    #region Model Governance

    /// <summary>
    /// Model governance and lifecycle management
    /// </summary>
    public interface ITransformerGovernance
    {
        Task<GovernanceReport> GenerateGovernanceReportAsync(string modelId);
        Task<bool> ApproveModelAsync(string modelId, ApprovalRequest request);
        Task<bool> RejectModelAsync(string modelId, RejectionReason reason);
        Task<ModelLifecycle> GetModelLifecycleAsync(string modelId);
        Task<bool> PromoteModelAsync(string modelId, Environment targetEnvironment);
        Task<bool> RetireModelAsync(string modelId, RetirementReason reason);
        Task<List<ModelReview>> GetPendingReviewsAsync();
        Task<bool> PerformModelValidationAsync(string modelId, ValidationCriteria criteria);
    }

    /// <summary>
    /// Model governance report
    /// </summary>
    public class GovernanceReport
    {
        public string ModelId { get; set; } = string.Empty;
        public ModelGovernanceStatus Status { get; set; }
        public List<GovernanceControl> Controls { get; set; } = new();
        public ModelDocumentation Documentation { get; set; } = new();
        public List<Stakeholder> Stakeholders { get; set; } = new();
        public RiskAssessment RiskAssessment { get; set; } = new();
        public List<GovernanceGap> Gaps { get; set; } = new();
        public DateTime LastReview { get; set; }
        public DateTime NextReviewDue { get; set; }
    }

    /// <summary>
    /// Governance control
    /// </summary>
    public class GovernanceControl
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool IsImplemented { get; set; }
        public DateTime ImplementationDate { get; set; }
        public string Owner { get; set; } = string.Empty;
        public List<Evidence> Evidence { get; set; } = new();
        public DateTime LastReview { get; set; }
    }

    /// <summary>
    /// Stakeholder information
    /// </summary>
    public class Stakeholder
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public List<string> Responsibilities { get; set; } = new();
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Risk assessment
    /// </summary>
    public class RiskAssessment
    {
        public string Id { get; set; } = string.Empty;
        public SecurityRating OverallRisk { get; set; }
        public List<IdentifiedRisk> Risks { get; set; } = new();
        public List<string> MitigationStrategies { get; set; } = new();
        public DateTime AssessmentDate { get; set; }
        public string AssessedBy { get; set; } = string.Empty;
        public DateTime NextAssessmentDue { get; set; }
    }

    /// <summary>
    /// Identified risk
    /// </summary>
    public class IdentifiedRisk
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public SecurityRating Severity { get; set; }
        public double Probability { get; set; }
        public string Category { get; set; } = string.Empty;
        public List<string> MitigationActions { get; set; } = new();
        public string Owner { get; set; } = string.Empty;
        public DateTime TargetResolutionDate { get; set; }
    }

    /// <summary>
    /// Governance gap
    /// </summary>
    public class GovernanceGap
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string RequiredControl { get; set; } = string.Empty;
        public string CurrentState { get; set; } = string.Empty;
        public string RequiredState { get; set; } = string.Empty;
        public SecurityRating Impact { get; set; }
        public List<string> RemediationSteps { get; set; } = new();
        public DateTime TargetCompletionDate { get; set; }
        public string Owner { get; set; } = string.Empty;
    }

    /// <summary>
    /// Model documentation requirements
    /// </summary>
    public class ModelDocumentation
    {
        public string Purpose { get; set; } = string.Empty;
        public string BusinessCase { get; set; } = string.Empty;
        public List<string> Limitations { get; set; } = new();
        public List<string> BiasConsiderations { get; set; } = new();
        public PerformanceMetrics PerformanceMetrics { get; set; } = new();
        public List<string> EthicalConsiderations { get; set; } = new();
        public string ModelCard { get; set; } = string.Empty; // Link to model card
        public List<string> References { get; set; } = new();
        public DateTime LastUpdated { get; set; }
    }

    /// <summary>
    /// Performance metrics
    /// </summary>
    public class PerformanceMetrics
    {
        public Dictionary<string, double> Metrics { get; set; } = new();
        public DateTime MeasuredAt { get; set; }
        public string TestDataset { get; set; } = string.Empty;
        public string EvaluationMethod { get; set; } = string.Empty;
        public List<string> Assumptions { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Approval request
    /// </summary>
    public class ApprovalRequest
    {
        public string Id { get; set; } = string.Empty;
        public string RequesterId { get; set; } = string.Empty;
        public DateTime RequestDate { get; set; }
        public string Justification { get; set; } = string.Empty;
        public Environment TargetEnvironment { get; set; }
        public List<string> RequiredApprovers { get; set; } = new();
        public List<ApprovalResponse> Approvals { get; set; } = new();
        public string Status { get; set; } = string.Empty;
    }

    /// <summary>
    /// Approval response
    /// </summary>
    public class ApprovalResponse
    {
        public string ApproverId { get; set; } = string.Empty;
        public bool IsApproved { get; set; }
        public DateTime ResponseDate { get; set; }
        public string Comments { get; set; } = string.Empty;
        public List<string> Conditions { get; set; } = new();
    }

    /// <summary>
    /// Rejection reason
    /// </summary>
    public class RejectionReason
    {
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> RequiredChanges { get; set; } = new();
        public string RejectedBy { get; set; } = string.Empty;
        public DateTime RejectionDate { get; set; }
        public bool CanResubmit { get; set; } = true;
    }

    /// <summary>
    /// Model lifecycle tracking
    /// </summary>
    public class ModelLifecycle
    {
        public string ModelId { get; set; } = string.Empty;
        public ModelGovernanceStatus CurrentStatus { get; set; }
        public List<LifecycleStage> Stages { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdated { get; set; }
        public string CurrentEnvironment { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new();
    }

    /// <summary>
    /// Lifecycle stage
    /// </summary>
    public class LifecycleStage
    {
        public string StageName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string CompletedBy { get; set; } = string.Empty;
        public List<string> Artifacts { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Retirement reason
    /// </summary>
    public class RetirementReason
    {
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime RetirementDate { get; set; }
        public string ReplacementModel { get; set; } = string.Empty;
        public List<string> MigrationSteps { get; set; } = new();
        public string RetiredBy { get; set; } = string.Empty;
    }

    /// <summary>
    /// Model review
    /// </summary>
    public class ModelReview
    {
        public string Id { get; set; } = string.Empty;
        public string ModelId { get; set; } = string.Empty;
        public string ReviewType { get; set; } = string.Empty;
        public DateTime ScheduledDate { get; set; }
        public string Reviewer { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public List<string> ReviewCriteria { get; set; } = new();
        public string Comments { get; set; } = string.Empty;
        public bool IsApproved { get; set; }
    }

    /// <summary>
    /// Validation criteria
    /// </summary>
    public class ValidationCriteria
    {
        public List<string> RequiredTests { get; set; } = new();
        public Dictionary<string, double> PerformanceThresholds { get; set; } = new();
        public List<string> ComplianceRequirements { get; set; } = new();
        public bool RequireBiasAssessment { get; set; } = true;
        public bool RequireExplainabilityTest { get; set; } = true;
        public List<string> SecurityChecks { get; set; } = new();
        public Dictionary<string, object> CustomCriteria { get; set; } = new();
    }

    public enum ModelGovernanceStatus
    {
        Draft,
        UnderReview,
        Approved,
        Rejected,
        Deprecated,
        Retired
    }

    public enum Environment
    {
        Development,
        Testing,
        Staging,
        Production
    }

    #endregion
}