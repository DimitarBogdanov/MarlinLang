; ModuleID = 'HelloWorld'
source_filename = "HelloWorld"

define i32 @__global__.Test.main() {
entry:
  ret i32 fadd (i32 1, i32 1)
}
