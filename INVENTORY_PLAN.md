# Level 3: Quantity-Based Inventory System

## Current State Assessment
**Current Level**: Level 2 - Individual Item Tracking with Category Management

✅ **What we have:**
- Hardware Types as categories/templates with dynamic property schemas
- Individual hardware items with unique tracking (serial numbers, properties, assignments)
- Purchase information and asset tracking per item
- Status management and assignment history per item
- Multi-tenant isolation

## Target State: Level 3 - Quantity-Based Inventory

### Core Concept Change
**Current**: Every piece of equipment is an individual `Hardware` entity
**Level 3**: Distinguish between **Individual Assets** (tracked) vs **Inventory Items** (quantities)

---

## 1. Database Schema Changes

### New Entity: `InventoryItem`
```csharp
public class InventoryItem : BaseEntity
{
    public Guid HardwareTypeId { get; set; }
    public int StockQuantity { get; set; }           // Total owned
    public int AvailableQuantity { get; set; }       // Available for assignment
    public int AssignedQuantity { get; set; }        // Currently assigned
    public int ReservedQuantity { get; set; }        // Reserved for events
    public int MinimumStock { get; set; }            // Reorder point
    public int MaximumStock { get; set; }            // Max to maintain
    public decimal UnitCost { get; set; }            // Cost per item
    public string? Location { get; set; }            // Storage location
    public bool RequiresIndividualTracking { get; set; } // When to create Hardware items
    
    // Navigation properties
    public HardwareType HardwareType { get; set; } = null!;
    public List<StockMovement> Movements { get; set; } = new();
    public List<BulkAssignment> BulkAssignments { get; set; } = new();
}
```

### New Entity: `StockMovement`
```csharp
public class StockMovement : BaseEntity
{
    public Guid InventoryItemId { get; set; }
    public StockMovementType Type { get; set; }      // Purchase, Assignment, Return, Loss, etc.
    public int Quantity { get; set; }               // +/- quantity
    public string Reason { get; set; }              // Why this movement occurred
    public Guid? RelatedEntityId { get; set; }      // Assignment, Purchase Order, etc.
    public string? RelatedEntityType { get; set; }   // "Assignment", "PurchaseOrder"
    public decimal? UnitCost { get; set; }           // For purchases
    public Guid? PerformedByUserId { get; set; }     // User who made the change
    public string? Notes { get; set; }               // Additional details
    
    // Navigation properties
    public InventoryItem InventoryItem { get; set; } = null!;
    public User? PerformedByUser { get; set; }
}

public enum StockMovementType
{
    Purchase,       // New stock acquired
    Assignment,     // Assigned to member
    Return,         // Returned from member
    Loss,          // Lost or stolen
    Damage,        // Damaged and removed from service
    Transfer,      // Moved between locations
    Adjustment,    // Manual stock correction
    Disposal       // Disposed/retired
}
```

### New Entity: `BulkAssignment`
```csharp
public class BulkAssignment : BaseEntity
{
    public Guid InventoryItemId { get; set; }
    public Guid MemberId { get; set; }
    public int Quantity { get; set; }
    public AssignmentStatus Status { get; set; }
    public DateTime AssignedAt { get; set; }
    public DateTime? ReturnedAt { get; set; }
    public DateTime? DueDate { get; set; }
    public Guid AssignedByUserId { get; set; }
    public Guid? ReturnedByUserId { get; set; }
    public string? AssignmentNotes { get; set; }
    public string? ReturnNotes { get; set; }
    public decimal? LateFee { get; set; }
    public decimal? DamageFee { get; set; }
    
    // Navigation properties
    public InventoryItem InventoryItem { get; set; } = null!;
    public Member Member { get; set; } = null!;
    public User AssignedByUser { get; set; } = null!;
    public User? ReturnedByUser { get; set; }
}
```

### Modified: `HardwareType`
```csharp
public class HardwareType : BaseEntity
{
    // ... existing properties
    public InventoryMethod InventoryMethod { get; set; } // Individual, Quantity, Hybrid
    public bool AllowBulkAssignment { get; set; }
    public int? DefaultMinimumStock { get; set; }
    public int? DefaultMaximumStock { get; set; }
    public bool AutoCreateIndividualItems { get; set; }   // Create Hardware items for high-value items
    
    // Navigation properties  
    public InventoryItem? InventoryItem { get; set; }    // One-to-one for quantity-based types
}

public enum InventoryMethod
{
    Individual,    // Track every item (current system)
    Quantity,      // Track only quantities  
    Hybrid         // Some individual, some quantity
}
```

### New Entity: `StockReservation` 
```csharp
public class StockReservation : BaseEntity
{
    public Guid InventoryItemId { get; set; }
    public int Quantity { get; set; }
    public DateTime ReservedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public Guid ReservedByUserId { get; set; }
    public string Reason { get; set; } = string.Empty;  // "Assignment Processing", "Event Planning"
    public Guid? RelatedEntityId { get; set; }
    public string? RelatedEntityType { get; set; }
    
    // Navigation properties
    public InventoryItem InventoryItem { get; set; } = null!;
    public User ReservedByUser { get; set; } = null!;
}
```

---

## 2. Assignment System Changes

### Hybrid Assignment Logic
```csharp
public class AssignmentRequest
{
    public Guid HardwareTypeId { get; set; }
    public int Quantity { get; set; } = 1;
    public Guid? SpecificHardwareId { get; set; }    // For individual tracking
    public AssignmentMode Mode { get; set; }         // Individual vs Bulk
    public Guid MemberId { get; set; }
    public DateTime? DueDate { get; set; }
    public string? Notes { get; set; }
}

public enum AssignmentMode
{
    Individual,    // Assign specific hardware item
    Bulk          // Assign from available quantity
}
```

### Stock Reservation System
- Reserve quantities during assignment process
- Handle stock conflicts (multiple users trying to assign last items)
- Automatic unreservation if assignment fails/expires
- Reservation expiry cleanup job

---

## 3. API Endpoints Needed

### Inventory Management Controllers

#### `InventoryController.cs`
```csharp
[HttpGet]                                    // Get all inventory items
[HttpGet("{hardwareTypeId}")]               // Get specific inventory item
[HttpPost("{hardwareTypeId}/adjust")]       // Manual stock adjustment
[HttpGet("low-stock")]                      // Items below minimum stock
[HttpGet("movements")]                      // Stock movement history
[HttpPost("purchase")]                      // Record new stock purchase
[HttpPost("reserve")]                       // Reserve stock
[HttpDelete("reservation/{id}")]            // Cancel reservation
```

#### `BulkAssignmentController.cs`
```csharp
[HttpPost]                                  // Create bulk assignment
[HttpPost("bulk-return")]                   // Process bulk returns
[HttpGet("member/{memberId}")]              // Get member's bulk assignments
[HttpGet("overdue")]                        // Get overdue bulk assignments
[HttpPut("{id}/extend")]                    // Extend due date
```

### New DTOs
```csharp
public class InventoryItemDto
{
    public Guid Id { get; set; }
    public Guid HardwareTypeId { get; set; }
    public string HardwareTypeName { get; set; } = string.Empty;
    public string HardwareTypeIcon { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    public int AvailableQuantity { get; set; }
    public int AssignedQuantity { get; set; }
    public int ReservedQuantity { get; set; }
    public int MinimumStock { get; set; }
    public int MaximumStock { get; set; }
    public decimal UnitCost { get; set; }
    public string? Location { get; set; }
    public bool IsLowStock { get; set; }
    public bool IsOutOfStock { get; set; }
    public decimal TotalValue { get; set; }
    public List<StockMovementDto> RecentMovements { get; set; } = new();
}

public class StockMovementDto
{
    public Guid Id { get; set; }
    public StockMovementType Type { get; set; }
    public int Quantity { get; set; }
    public string Reason { get; set; } = string.Empty;
    public decimal? UnitCost { get; set; }
    public string? PerformedByUserName { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? Notes { get; set; }
}

public class BulkAssignmentDto
{
    public Guid Id { get; set; }
    public Guid InventoryItemId { get; set; }
    public string HardwareTypeName { get; set; } = string.Empty;
    public string HardwareTypeIcon { get; set; } = string.Empty;
    public Guid MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public string MemberEmail { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public AssignmentStatus Status { get; set; }
    public DateTime AssignedAt { get; set; }
    public DateTime? ReturnedAt { get; set; }
    public DateTime? DueDate { get; set; }
    public bool IsOverdue { get; set; }
    public int DaysAssigned { get; set; }
    public string AssignedByUserName { get; set; } = string.Empty;
    public string? ReturnedByUserName { get; set; }
    public string? AssignmentNotes { get; set; }
    public string? ReturnNotes { get; set; }
    public decimal? LateFee { get; set; }
    public decimal? DamageFee { get; set; }
}

public class BulkAssignmentRequest
{
    public Guid HardwareTypeId { get; set; }
    public List<Guid> MemberIds { get; set; } = new();
    public int QuantityPerMember { get; set; } = 1;
    public DateTime? DueDate { get; set; }
    public string? Notes { get; set; }
}

public class StockAdjustmentRequest
{
    public int QuantityChange { get; set; }  // +/- adjustment
    public string Reason { get; set; } = string.Empty;
    public decimal? UnitCost { get; set; }
    public string? Notes { get; set; }
}
```

---

## 4. UI Changes Required

### New Pages/Components

#### Inventory Dashboard (`/inventory`)
- Stock levels overview with visual indicators
- Low stock alerts and notifications
- Total inventory value and key metrics
- Quick actions (adjust stock, bulk assign, etc.)
- Recent stock movements feed

#### Stock Management (`/inventory/{hardwareTypeId}`)
- Detailed stock information for specific equipment type
- Stock movement history with filtering
- Manual stock adjustments
- Bulk assignment management
- Reorder point configuration

#### Bulk Assignment (`/assignments/bulk`)
- Multi-member assignment interface
- Equipment quantity selection
- Due date and notes management
- Batch processing status

#### Inventory Reports (`/inventory/reports`)
- Usage pattern analysis
- Stock turnover rates
- Asset utilization metrics
- Member usage statistics
- Financial reports (asset value, costs)

### Modified Hardware Listing
```razor
<!-- Current: Individual items only -->
<MudDataGrid Items="@_hardware" />

<!-- Level 3: Mixed view -->
<MudTabs>
    <MudTabPanel Text="Individual Items">
        @foreach (var item in _individualHardware)
        {
            <HardwareItemRow Item="@item" />
        }
    </MudTabPanel>
    
    <MudTabPanel Text="Inventory Items">
        @foreach (var inventory in _inventoryItems)
        {
            <InventoryItemRow Item="@inventory" />
        }
    </MudTabPanel>
    
    <MudTabPanel Text="All Equipment">
        @foreach (var type in _hardwareTypes)
        {
            @if (type.InventoryMethod == InventoryMethod.Individual)
            {
                @foreach (var item in type.Hardware)
                {
                    <HardwareItemRow Item="@item" ShowType="false" />
                }
            }
            else
            {
                <InventoryItemRow Item="@type.InventoryItem" />
            }
        }
    </MudTabPanel>
</MudTabs>
```

### New UI Components
- `InventoryItemCard.razor` - Summary card with stock levels
- `StockLevelIndicator.razor` - Visual stock level gauge
- `BulkAssignmentDialog.razor` - Multi-member assignment modal
- `StockMovementHistory.razor` - Movement log display
- `InventoryDashboardCards.razor` - Key metrics widgets

---

## 5. Business Logic Complexity

### Assignment Decision Engine
```csharp
private async Task<AssignmentResult> AssignHardwareAsync(AssignmentRequest request)
{
    var hardwareType = await GetHardwareTypeAsync(request.HardwareTypeId);
    
    return hardwareType.InventoryMethod switch
    {
        InventoryMethod.Individual => await AssignIndividualItemAsync(request),
        InventoryMethod.Quantity => await AssignFromInventoryAsync(request),
        InventoryMethod.Hybrid => await AssignHybridAsync(request),
        _ => throw new InvalidOperationException($"Unknown inventory method: {hardwareType.InventoryMethod}")
    };
}

private async Task<AssignmentResult> AssignFromInventoryAsync(AssignmentRequest request)
{
    using var transaction = await _context.Database.BeginTransactionAsync();
    try
    {
        // 1. Check availability
        var inventory = await GetInventoryItemAsync(request.HardwareTypeId);
        if (inventory.AvailableQuantity < request.Quantity)
            return AssignmentResult.Failed("Insufficient stock available");

        // 2. Create reservation
        var reservation = await CreateStockReservationAsync(inventory.Id, request.Quantity, request.UserId);

        // 3. Create bulk assignment
        var bulkAssignment = new BulkAssignment
        {
            InventoryItemId = inventory.Id,
            MemberId = request.MemberId,
            Quantity = request.Quantity,
            Status = AssignmentStatus.Active,
            AssignedAt = DateTime.UtcNow,
            DueDate = request.DueDate,
            AssignedByUserId = request.UserId,
            AssignmentNotes = request.Notes
        };

        // 4. Update inventory quantities
        inventory.AssignedQuantity += request.Quantity;
        inventory.AvailableQuantity -= request.Quantity;

        // 5. Record stock movement
        var movement = new StockMovement
        {
            InventoryItemId = inventory.Id,
            Type = StockMovementType.Assignment,
            Quantity = -request.Quantity,
            Reason = $"Bulk assignment to member {request.MemberName}",
            RelatedEntityId = bulkAssignment.Id,
            RelatedEntityType = nameof(BulkAssignment),
            PerformedByUserId = request.UserId
        };

        _context.BulkAssignments.Add(bulkAssignment);
        _context.StockMovements.Add(movement);
        await _context.SaveChangesAsync();
        
        // 6. Release reservation
        await ReleaseStockReservationAsync(reservation.Id);
        
        await transaction.CommitAsync();
        return AssignmentResult.Success(bulkAssignment.Id);
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync();
        return AssignmentResult.Failed($"Assignment failed: {ex.Message}");
    }
}
```

### Stock Level Management Services
```csharp
public interface IInventoryService
{
    Task<InventoryItemDto> GetInventoryItemAsync(Guid hardwareTypeId);
    Task<List<InventoryItemDto>> GetLowStockItemsAsync();
    Task<Result> AdjustStockAsync(Guid hardwareTypeId, StockAdjustmentRequest request);
    Task<Result> RecordPurchaseAsync(Guid hardwareTypeId, int quantity, decimal unitCost);
    Task<StockReservation> ReserveStockAsync(Guid hardwareTypeId, int quantity, string reason);
    Task ReleaseReservationAsync(Guid reservationId);
    Task ProcessExpiredReservationsAsync(); // Background job
}

public interface IBulkAssignmentService  
{
    Task<Result<BulkAssignmentDto>> CreateBulkAssignmentAsync(BulkAssignmentRequest request);
    Task<Result> ProcessBulkReturnAsync(Guid assignmentId, string? returnNotes);
    Task<List<BulkAssignmentDto>> GetMemberBulkAssignmentsAsync(Guid memberId);
    Task<List<BulkAssignmentDto>> GetOverdueBulkAssignmentsAsync();
}
```

### Conflict Resolution
- Optimistic concurrency control for stock adjustments
- Reservation system prevents double-assignment
- Transaction isolation for critical stock operations
- Background cleanup of expired reservations

---

## 6. Migration Strategy

### Phase 1: Foundation (2-3 weeks)
**Goal**: Add inventory entities without breaking current system

**Tasks**:
- [ ] Create new database entities (`InventoryItem`, `StockMovement`, `BulkAssignment`, `StockReservation`)
- [ ] Add migration scripts
- [ ] Create inventory services interfaces
- [ ] Add `InventoryMethod` to `HardwareType`
- [ ] Populate initial inventory data from existing hardware
- [ ] Create background job for reservation cleanup

**Testing**: Verify new entities work alongside existing system

### Phase 2: Hybrid Mode (3-4 weeks)  
**Goal**: Support both individual and quantity-based items

**Tasks**:
- [ ] Implement inventory management APIs
- [ ] Create bulk assignment logic
- [ ] Build basic inventory UI components
- [ ] Add inventory dashboard page
- [ ] Create stock adjustment workflows
- [ ] Implement reservation system

**Testing**: Test both assignment modes work correctly

### Phase 3: Full Integration (4-5 weeks)
**Goal**: Complete UI overhaul and advanced features

**Tasks**:
- [ ] Redesign hardware listing with mixed views  
- [ ] Build comprehensive inventory management UI
- [ ] Add bulk assignment interfaces
- [ ] Implement inventory reports and analytics
- [ ] Add low stock alerts and notifications
- [ ] Create procurement workflow integration
- [ ] Performance optimization for large inventories

**Testing**: End-to-end testing of complete inventory system

### Phase 4: Advanced Features (2-3 weeks)
**Goal**: Business intelligence and optimization

**Tasks**:
- [ ] Usage pattern analysis
- [ ] Predictive reorder suggestions
- [ ] Asset utilization reports
- [ ] Cost tracking and ROI analysis
- [ ] Multi-location support (if needed)
- [ ] Integration with financial systems

---

## 7. Technical Considerations

### Database Performance
- Index strategy for inventory queries
- Partitioning for stock movement history
- Caching for frequently accessed inventory data
- Bulk operation optimizations

### Concurrency Control
- Optimistic concurrency for inventory updates
- Distributed locking for critical stock operations
- Event sourcing for stock movement audit trail
- ACID compliance for assignment transactions

### Security & Permissions
- Role-based access to inventory management
- Audit logging for all stock movements
- Separation of duties (assign vs adjust stock)
- Tenant isolation for multi-tenant inventory

### Scalability
- Horizontal scaling for high-volume stock movements
- Event-driven architecture for inventory updates
- Background processing for bulk operations
- Read replicas for reporting queries

---

## 8. Complexity Assessment

### Development Effort: **HIGH**
- **Database**: 40+ hours (entities, migrations, seed data)
- **Backend**: 80+ hours (services, APIs, business logic)
- **Frontend**: 100+ hours (new pages, components, workflows)
- **Testing**: 60+ hours (unit, integration, E2E tests)
- **Documentation**: 20+ hours

**Total Estimated Effort**: 300+ hours (7-8 weeks for experienced team)

### Ongoing Maintenance: **MEDIUM-HIGH**
- Background job monitoring
- Inventory data reconciliation
- Performance tuning for large datasets
- Business rule maintenance
- User training and support

### Business Impact: **VERY HIGH**
- Enables bulk equipment management
- Reduces administrative overhead
- Improves stock visibility and control
- Supports better financial planning
- Enables data-driven equipment decisions

---

## 9. Decision Framework

### Implement Level 3 Inventory IF:
✅ You manage large quantities of identical/similar items
✅ Individual tracking of every item is overkill for some equipment
✅ You need better stock control and visibility
✅ Bulk assignments are common business operations
✅ You want inventory-based financial reporting
✅ You have development resources for major upgrade

### Stay at Level 2 IF:
❌ Most equipment requires individual tracking
❌ Stock quantities are generally low
❌ Current system meets business needs adequately
❌ Limited development resources available
❌ Other features have higher business priority

---

## 10. Recommended Next Steps

1. **Business Analysis** (1 week)
   - Catalog current equipment types by tracking needs
   - Identify candidates for quantity-based management
   - Assess bulk assignment frequency and patterns

2. **Proof of Concept** (2 weeks)
   - Implement core inventory entities
   - Create simple stock adjustment API
   - Build basic inventory display component

3. **Stakeholder Review** (1 week)
   - Demo POC to key users
   - Gather feedback on UI/UX approach  
   - Validate business process alignment

4. **Go/No-Go Decision**
   - Assess POC feedback
   - Confirm development resource availability
   - Prioritize against other system needs

This inventory upgrade would be a **major system evolution** that significantly enhances the platform's capabilities but requires substantial development investment.