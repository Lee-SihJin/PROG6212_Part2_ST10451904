# PROG6212 Part 2 - ST10451904
# Contract Monthly Claim System

## Overview

The Contract Monthly Claim System is a comprehensive web application designed to manage monthly claims for contract lecturers. The system facilitates the submission, review, approval, and tracking of claims through a multi-level approval workflow involving lecturers, coordinators, and managers.

## System Architecture

### User Roles

The system supports three distinct user roles:

#### Lecturer
- Submit monthly claims with supporting documents
- Save claims as drafts for later editing
- Track claim status and view history
- Download submitted documents

#### Coordinator
- Review and approve/reject submitted claims
- View supporting documents
- Process claims with optional comments
- Monitor pending and processed claims

#### Manager
- Final approval authority for claims
- Review coordinator decisions
- Access comprehensive reporting and analytics
- Oversee the entire claims process

## Features

### Claim Management
- **Multi-step Submission**: Claims can be saved as drafts or submitted directly
- **Document Support**: Upload multiple supporting documents (PDF, Word, Images)
- **Real-time Calculations**: Automatic calculation of claim amounts based on hours and hourly rates
- **Status Tracking**: Visual timeline showing claim progression through approval stages

### Document Handling
- **File Upload**: Support for PDF, DOC/DOCX, JPG, PNG files (max 10MB each)
- **Document Preview**: In-browser preview for PDF and image files
- **Bulk Downloads**: Download all documents for a claim in one action
- **Document Management**: Add, remove, and categorize supporting documents

### Approval Workflow
- **Two-Level Approval**: Coordinator review followed by manager final approval
- **Decision Tracking**: Complete audit trail of all approval decisions
- **Notes and Comments**: Optional notes for approvals, required reasons for rejections
- **Status Badges**: Visual indicators for claim status throughout the process

### Dashboard Features
- **Role-based Views**: Customized dashboards for each user type
- **Quick Statistics**: Overview of pending, approved, and total claims
- **Recent Activity**: Latest claims and actions for quick access
- **Performance Metrics**: Processing times and approval rates

## Technical Implementation

### Frontend Technologies
- **Bootstrap 5**: Responsive UI framework
- **Font Awesome**: Icon library for visual elements
- **JavaScript**: Client-side form validation and interactivity
- **Razor Pages**: ASP.NET Core server-side rendering

### Key Components

#### Lecturer Interface
- **Dashboard**: Claim submission form with real-time amount calculation
- **Document Management**: Dynamic document upload and management
- **Claim History**: Table view of all claims with status indicators
- **Edit Functionality**: Draft claim editing with document management

#### Coordinator Interface
- **Pending Claims**: List of claims requiring approval
- **Claim Review**: Detailed view with document preview capabilities
- **Approval Actions**: Modal-based approval/rejection with reason tracking
- **Processed Claims**: History of coordinator decisions

#### Manager Interface
- **Final Approval**: Claims awaiting manager decision
- **Comprehensive Review**: Coordinator decisions and supporting documents
- **Reporting**: Monthly overview and processing statistics
- **Audit Trail**: Complete decision history

### Security Features
- **Anti-Forgery Tokens**: Protection against CSRF attacks
- **Role-based Access**: Controller-level authorization
- **Input Validation**: Server-side and client-side validation
- **Secure File Handling**: Safe document upload and download

## File Structure

The system consists of multiple Razor views organized by user role:

- **Lecturer Views**: `Index.cshtml`, `ClaimDetails.cshtml`, `EditDraft.cshtml`
- **Coordinator Views**: `Index.cshtml`, `ClaimDetails.cshtml`, `ProcessedClaimDetails.cshtml`, `ProcessedClaims.cshtml`
- **Manager Views**: `Index.cshtml`, `ClaimDetails.cshtml`, `ProcessedClaimDetails.cshtml`, `ProcessedClaims.cshtml`
- **Shared Views**: `Login.cshtml`

## Claim Status Lifecycle

1. **Draft** - Initial state when lecturer saves without submission
2. **Submitted** - Claim submitted for coordinator review
3. **Coordinator Approved** - Approved by coordinator, awaiting manager review
4. **Manager Approved** - Final approval granted by manager
5. **Rejected** - Claim rejected at any stage
6. **Paid** - Final state after payment processing

## Usage Guidelines

### For Lecturers
- Submit claims by the end of each month
- Ensure all supporting documents are attached
- Use draft feature for incomplete claims
- Monitor claim status through the dashboard

### For Coordinators
- Review claims promptly upon submission
- Verify supporting documentation thoroughly
- Provide clear reasons for any rejections
- Process claims within established timeframes

### For Managers
- Conduct final review of coordinator-approved claims
- Maintain oversight of claim processing times
- Use reporting features for performance monitoring
- Ensure compliance with institutional policies
