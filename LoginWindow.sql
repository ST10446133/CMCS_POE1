
USE POE_CMCS;
GO

-- Create simple Users table (no hashing, no full name)
CREATE TABLE Users (
    UserId INT IDENTITY(1,1) PRIMARY KEY,
    Username NVARCHAR(100) NOT NULL UNIQUE,
    Password NVARCHAR(100) NOT NULL,
    Role NVARCHAR(50) NOT NULL
);
GO

-- Insert example users
INSERT INTO Users (Username, Password, Role)
VALUES
('lecturer1', '12345', 'Lecturer'),
('coordinator1', 'admin123', 'Coordinator'),
('manager1', 'managerpass', 'Manager');
GO

CREATE TABLE Claims (
    ClaimId INT IDENTITY(1,1) PRIMARY KEY,
    LecturerUsername NVARCHAR(100) NOT NULL,
    HoursWorked DECIMAL(10,2) NOT NULL,
    HourlyRate DECIMAL(10,2) NOT NULL,
    Notes NVARCHAR(500),
    SupportingDoc NVARCHAR(255),
    FilePath NVARCHAR(500),
    Status NVARCHAR(50) DEFAULT 'Pending',
    DateSubmitted DATETIME DEFAULT GETDATE()
);
ALTER TABLE Claims ALTER COLUMN Status NVARCHAR(50);
GO