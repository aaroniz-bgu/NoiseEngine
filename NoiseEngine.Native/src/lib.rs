use interop::interop_allocator::InteropAllocator;

#[global_allocator]
static GLOBAL_ALLOCATOR: InteropAllocator = InteropAllocator;

pub mod errors;
pub mod interop;
pub mod logging;
pub mod rendering;
pub mod serialization;
