-- Initial database setup for Club Management Platform
-- This script runs when PostgreSQL container starts for the first time

-- Create extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pg_trgm";

-- Create main schema for tenant management (shared across all tenants)
CREATE SCHEMA IF NOT EXISTS public;

-- Create tenants table in public schema
CREATE TABLE IF NOT EXISTS public.tenants (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id UUID NOT NULL DEFAULT uuid_generate_v4(),
    name VARCHAR(255) NOT NULL,
    schema_name VARCHAR(63) NOT NULL UNIQUE,
    domain VARCHAR(255) NOT NULL UNIQUE,
    status VARCHAR(50) NOT NULL DEFAULT 'Active',
    branding JSONB DEFAULT '{}',
    plan VARCHAR(50) NOT NULL DEFAULT 'Basic',
    trial_ends_at TIMESTAMP NULL,
    max_members INTEGER NOT NULL DEFAULT 100,
    max_facilities INTEGER NOT NULL DEFAULT 10,
    max_staff INTEGER NOT NULL DEFAULT 5,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NULL,
    created_by VARCHAR(255) NULL,
    updated_by VARCHAR(255) NULL
);

-- Create indexes on tenants table
CREATE INDEX IF NOT EXISTS idx_tenants_domain ON public.tenants(domain);
CREATE INDEX IF NOT EXISTS idx_tenants_schema_name ON public.tenants(schema_name);
CREATE INDEX IF NOT EXISTS idx_tenants_status ON public.tenants(status);

-- Create demo tenant for development
INSERT INTO public.tenants (
    name, 
    schema_name, 
    domain, 
    status, 
    branding,
    plan,
    max_members,
    max_facilities,
    max_staff
) VALUES (
    'Demo Sports Club',
    'demo_club',
    'demo.localhost',
    'Active',
    '{"primaryColor": "#1976d2", "secondaryColor": "#dc004e", "companyName": "Demo Sports Club"}',
    'Premium',
    500,
    25,
    15
) ON CONFLICT (domain) DO NOTHING;

-- Create the demo tenant schema
CREATE SCHEMA IF NOT EXISTS demo_club;

-- Function to create tenant schema with all required tables
CREATE OR REPLACE FUNCTION create_tenant_schema(schema_name TEXT)
RETURNS VOID AS $$
BEGIN
    -- Create schema
    EXECUTE format('CREATE SCHEMA IF NOT EXISTS %I', schema_name);
    
    -- Set search path to the new schema
    EXECUTE format('SET search_path TO %I', schema_name);
    
    -- Create users table
    EXECUTE format('
        CREATE TABLE IF NOT EXISTS %I.users (
            id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
            tenant_id UUID NOT NULL,
            email VARCHAR(255) NOT NULL,
            first_name VARCHAR(255) NOT NULL,
            last_name VARCHAR(255) NOT NULL,
            phone_number VARCHAR(50) NOT NULL DEFAULT '''',
            role VARCHAR(50) NOT NULL DEFAULT ''Member'',
            status VARCHAR(50) NOT NULL DEFAULT ''Active'',
            profile_photo_url VARCHAR(500) NULL,
            last_login_at TIMESTAMP NULL,
            email_verified BOOLEAN NOT NULL DEFAULT false,
            custom_fields JSONB DEFAULT ''{}''::jsonb,
            created_at TIMESTAMP NOT NULL DEFAULT NOW(),
            updated_at TIMESTAMP NULL,
            created_by VARCHAR(255) NULL,
            updated_by VARCHAR(255) NULL
        )', schema_name);
    
    -- Create members table
    EXECUTE format('
        CREATE TABLE IF NOT EXISTS %I.members (
            id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
            tenant_id UUID NOT NULL,
            user_id UUID NOT NULL,
            membership_number VARCHAR(50) NOT NULL,
            tier VARCHAR(50) NOT NULL DEFAULT ''Basic'',
            status VARCHAR(50) NOT NULL DEFAULT ''Active'',
            joined_at TIMESTAMP NOT NULL DEFAULT NOW(),
            membership_expires_at TIMESTAMP NULL,
            last_visit_at TIMESTAMP NULL,
            balance DECIMAL(10,2) NOT NULL DEFAULT 0.00,
            emergency_contact JSONB NULL,
            medical_info JSONB NULL,
            custom_fields JSONB DEFAULT ''{}''::jsonb,
            created_at TIMESTAMP NOT NULL DEFAULT NOW(),
            updated_at TIMESTAMP NULL,
            created_by VARCHAR(255) NULL,
            updated_by VARCHAR(255) NULL
        )', schema_name);
    
    -- Create facility_types table
    EXECUTE format('
        CREATE TABLE IF NOT EXISTS %I.facility_types (
            id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
            tenant_id UUID NOT NULL,
            name VARCHAR(255) NOT NULL,
            description TEXT DEFAULT '''',
            icon VARCHAR(255) DEFAULT '''',
            property_schema JSONB DEFAULT ''{}''::jsonb,
            is_active BOOLEAN NOT NULL DEFAULT true,
            sort_order INTEGER NOT NULL DEFAULT 0,
            created_at TIMESTAMP NOT NULL DEFAULT NOW(),
            updated_at TIMESTAMP NULL,
            created_by VARCHAR(255) NULL,
            updated_by VARCHAR(255) NULL
        )', schema_name);
    
    -- Create facilities table
    EXECUTE format('
        CREATE TABLE IF NOT EXISTS %I.facilities (
            id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
            tenant_id UUID NOT NULL,
            name VARCHAR(255) NOT NULL,
            description TEXT DEFAULT '''',
            facility_type_id UUID NOT NULL,
            properties JSONB DEFAULT ''{}''::jsonb,
            status VARCHAR(50) NOT NULL DEFAULT ''Available'',
            capacity INTEGER NULL,
            hourly_rate DECIMAL(10,2) NULL,
            requires_booking BOOLEAN NOT NULL DEFAULT true,
            max_booking_days_in_advance INTEGER NOT NULL DEFAULT 30,
            min_booking_duration_minutes INTEGER NOT NULL DEFAULT 60,
            max_booking_duration_minutes INTEGER NOT NULL DEFAULT 180,
            operating_hours_start TIME NULL,
            operating_hours_end TIME NULL,
            operating_days JSONB DEFAULT ''[]''::jsonb,
            created_at TIMESTAMP NOT NULL DEFAULT NOW(),
            updated_at TIMESTAMP NULL,
            created_by VARCHAR(255) NULL,
            updated_by VARCHAR(255) NULL
        )', schema_name);
    
    -- Add indexes
    EXECUTE format('CREATE UNIQUE INDEX IF NOT EXISTS idx_%I_users_email_tenant ON %I.users(email, tenant_id)', schema_name, schema_name);
    EXECUTE format('CREATE UNIQUE INDEX IF NOT EXISTS idx_%I_members_number_tenant ON %I.members(membership_number, tenant_id)', schema_name, schema_name);
    EXECUTE format('CREATE INDEX IF NOT EXISTS idx_%I_facilities_type ON %I.facilities(facility_type_id)', schema_name, schema_name);
    
    -- Add foreign key constraints
    EXECUTE format('ALTER TABLE %I.members ADD CONSTRAINT IF NOT EXISTS fk_members_users FOREIGN KEY (user_id) REFERENCES %I.users(id) ON DELETE CASCADE', schema_name, schema_name);
    EXECUTE format('ALTER TABLE %I.facilities ADD CONSTRAINT IF NOT EXISTS fk_facilities_types FOREIGN KEY (facility_type_id) REFERENCES %I.facility_types(id) ON DELETE RESTRICT', schema_name, schema_name);
    
END;
$$ LANGUAGE plpgsql;

-- Create demo tenant schema with tables
SELECT create_tenant_schema('demo_club');

-- Insert demo data into demo_club schema
SET search_path TO demo_club;

-- Insert demo admin user
INSERT INTO users (
    tenant_id, 
    email, 
    first_name, 
    last_name, 
    phone_number, 
    role, 
    status, 
    email_verified
) VALUES (
    (SELECT id FROM public.tenants WHERE schema_name = 'demo_club'),
    'admin@demo.localhost',
    'Demo',
    'Admin',
    '+1-555-0123',
    'Admin',
    'Active',
    true
) ON CONFLICT DO NOTHING;

-- Insert demo facility types
INSERT INTO facility_types (
    tenant_id,
    name,
    description,
    icon,
    property_schema,
    sort_order
) VALUES 
(
    (SELECT id FROM public.tenants WHERE schema_name = 'demo_club'),
    'Tennis Court',
    'Professional tennis courts for singles and doubles play',
    'sports_tennis',
    '{"properties": [
        {"key": "surface_type", "label": "Surface Type", "type": "select", "required": true, "options": ["Clay", "Hard Court", "Grass", "Synthetic"]},
        {"key": "lighting", "label": "Has Lighting", "type": "boolean", "required": false},
        {"key": "net_height", "label": "Net Height (cm)", "type": "number", "required": false, "defaultValue": "91.4"}
    ]}',
    1
),
(
    (SELECT id FROM public.tenants WHERE schema_name = 'demo_club'),
    'Swimming Pool',
    'Indoor and outdoor swimming facilities',
    'pool',
    '{"properties": [
        {"key": "pool_type", "label": "Pool Type", "type": "select", "required": true, "options": ["Lap Pool", "Recreation Pool", "Kids Pool", "Diving Pool"]},
        {"key": "temperature", "label": "Temperature (°C)", "type": "number", "required": false},
        {"key": "depth", "label": "Depth (m)", "type": "number", "required": true},
        {"key": "heated", "label": "Heated", "type": "boolean", "required": false}
    ]}',
    2
),
(
    (SELECT id FROM public.tenants WHERE schema_name = 'demo_club'),
    'Fitness Center',
    'Modern fitness and gym facilities',
    'fitness_center',
    '{"properties": [
        {"key": "area_sqm", "label": "Area (sq m)", "type": "number", "required": false},
        {"key": "equipment_types", "label": "Available Equipment", "type": "multiselect", "required": false, "options": ["Cardio", "Weights", "Functional Training", "Group Fitness"]},
        {"key": "air_conditioning", "label": "Air Conditioning", "type": "boolean", "required": false}
    ]}',
    3
) ON CONFLICT DO NOTHING;

-- Insert demo facilities
INSERT INTO facilities (
    tenant_id,
    name,
    description,
    facility_type_id,
    properties,
    capacity,
    hourly_rate,
    operating_hours_start,
    operating_hours_end,
    operating_days
) VALUES 
(
    (SELECT id FROM public.tenants WHERE schema_name = 'demo_club'),
    'Center Court',
    'Main tennis court with premium surface and lighting',
    (SELECT id FROM facility_types WHERE name = 'Tennis Court'),
    '{"surface_type": "Clay", "lighting": true, "net_height": 91.4}',
    4,
    25.00,
    '06:00:00',
    '22:00:00',
    '["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"]'
),
(
    (SELECT id FROM public.tenants WHERE schema_name = 'demo_club'),
    'Court 2',
    'Standard tennis court for regular play',
    (SELECT id FROM facility_types WHERE name = 'Tennis Court'),
    '{"surface_type": "Hard Court", "lighting": true, "net_height": 91.4}',
    4,
    20.00,
    '06:00:00',
    '22:00:00',
    '["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"]'
),
(
    (SELECT id FROM public.tenants WHERE schema_name = 'demo_club'),
    'Olympic Pool',
    '50m Olympic-size swimming pool',
    (SELECT id FROM facility_types WHERE name = 'Swimming Pool'),
    '{"pool_type": "Lap Pool", "temperature": 26.5, "depth": 2.0, "heated": true}',
    50,
    15.00,
    '05:00:00',
    '23:00:00',
    '["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"]'
),
(
    (SELECT id FROM public.tenants WHERE schema_name = 'demo_club'),
    'Main Gym',
    'Fully equipped fitness center with modern equipment',
    (SELECT id FROM facility_types WHERE name = 'Fitness Center'),
    '{"area_sqm": 500, "equipment_types": ["Cardio", "Weights", "Functional Training"], "air_conditioning": true}',
    75,
    10.00,
    '05:00:00',
    '24:00:00',
    '["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"]'
) ON CONFLICT DO NOTHING;

-- Reset search path
SET search_path TO public;