// TODO
// [ ] crash on disassemble_cil with inline strings
// [ ] native disassemble method to byte array
// [ ] native disassemble method to string array
// [ ] native disassemble method to string
// [ ] verify method
// [ ] il disassemble method to string array
// [ ] il disassemble method to string

#include <stdio.h>
#include <inttypes.h>
#include <string.h>
#include <glib.h>

#include <mono/metadata/image.h>
#include <mono/metadata/verify.h>
#include <mono/metadata/class.h>
#include <mono/metadata/loader.h>
#include <mono/metadata/object.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/tokentype.h>

#include <dis/get.h>
#include <dis/dis-cil.h>

#include <Zydis/Zydis.h>
#include <Zydis/Utils.h>

// for monodis
gboolean substitute_with_mscorlib_p = FALSE;

static char*
token_handler (MonoDisHelper *dh, MonoMethod *method, guint32 token)
{
    MonoImage* image = mono_class_get_image( mono_method_get_class( method ));
    return get_token(image, token, NULL);
}

FILE* output;

void
disassemble_method_il_new (MonoMethod *method)
{
    init_key_table ();
    MonoImage* image = mono_class_get_image( mono_method_get_class( method ));
    mono_image_init(image);
    MonoMethodHeader *header = mono_method_get_header (method);
    
    output = stdout; //fmemopen(NULL, 1024*1024, "w");
    
    printf("%s:\n", mono_method_full_name(method, TRUE));
    disassemble_cil (image, header, NULL);
}

 
void
disassemble_method_il (MonoMethod *method)
{
    init_key_table ();
    
    MonoDisHelper dh;
    MonoMethodHeader *header = mono_method_get_header (method);
    
    memset (&dh, 0, sizeof (dh));
    dh.newline = "\n";
    dh.label_format = "IL_%04x: ";
    dh.label_target = "IL_%04x";
    dh.tokener = token_handler;
    
    const unsigned char* header_code;
    uint32_t header_size;
    header_code = mono_method_header_get_code(header, &header_size, NULL);
    char* il_code = mono_disasm_code(&dh, method, header_code, header_code + header_size);
    printf("%s:\n%s", mono_method_full_name(method, TRUE), il_code);
}

// void
// verify_method(MonoMethod* m)
// {
//     GSList* result = mono_method_verify(m, MONO_VERIFY_REPORT_ALL_ERRORS);
//     guint resultLength = g_slist_length (result);
    
//     printf("verify: %s\n", mono_method_full_name(m, TRUE));
//     if(resultLength == 0)
//     {
//         printf("verify OK\n");
//     }
    
//     // MonoArray* arr = mono_array_new(mono_domain_get (), mono_get_string_class (), resultLength);
//     for (int i = 0; i < resultLength; ++i)
//     {
//         MonoVerifyInfoExtended* r = g_slist_nth_data(result, i);
//         printf("bad verify: %s\n", r->info.message);
//         // MonoString* message = mono_string_new_wrapper(r->info.message);
//         // mono_array_set(arr, MonoString*, i, message);
//     }
//     // return arr;
// }

void
disassemble_method_il_old(MonoMethod* m)
{
    MonoMethodHeader *header = mono_method_get_header (m);
    const unsigned char* header_code;
    uint32_t header_size;
    header_code = mono_method_header_get_code(header, &header_size, NULL);
    char* il_code = mono_disasm_code(NULL, m, header_code, header_code + header_size);
    printf("%s:\n%s", mono_method_full_name(m, TRUE), il_code);
}

void
disassemble_method_native(MonoMethod* m)
{
    ZydisDecoder decoder;
    ZydisDecoderInit(
        &decoder,
        ZYDIS_MACHINE_MODE_LONG_64,
        ZYDIS_ADDRESS_WIDTH_64);
    
    ZydisFormatter formatter;
    ZydisFormatterInit(&formatter, ZYDIS_FORMATTER_STYLE_INTEL);
    
    gpointer code = mono_compile_method(m);
    MonoJitInfo* ji = mono_jit_info_table_find(mono_domain_get(), code);
    
    uint8_t* st = (uint8_t*)mono_jit_info_get_code_start(ji);
    int sz = mono_jit_info_get_code_size(ji);
    
    uint8_t* data = st;
    uint64_t instructionPointer = (uint64_t)code;
    size_t offset = 0;
    size_t length = sz;
    ZydisDecodedInstruction instruction;
    
    printf("%s:\n", mono_method_full_name(m, TRUE));
    while (ZYDIS_SUCCESS(ZydisDecoderDecodeBuffer(
        &decoder, data + offset, length - offset,
        instructionPointer, &instruction)))
    {
        printf("%016" PRIX64 "  ", instructionPointer);
        char buffer[256];
        ZydisFormatterFormatInstruction(
            &formatter, &instruction, buffer, sizeof(buffer));
        printf("%s", buffer);
        
        for(int i=0; i<instruction.operandCount; i++)
        {
            if(instruction.operands[i].type == ZYDIS_OPERAND_TYPE_IMMEDIATE)
            {
                // might be a jited method address
                MonoJitInfo* imji = mono_jit_info_table_find(mono_domain_get(), (char*)instruction.operands[i].imm.value.u);
                if(imji == NULL && instruction.operands[i].imm.isRelative)
                {
                    ZydisU64 absAddr;
                    ZydisCalcAbsoluteAddress(&instruction, &instruction.operands[i], &absAddr);
                    imji = mono_jit_info_table_find(mono_domain_get(), (char*)absAddr);
                }
                if(imji != NULL)
                {
                    MonoMethod* imjiMeth = mono_jit_info_get_method(imji);
                    printf(" # %s", mono_method_full_name(imjiMeth, TRUE));
                }
            }
        }
        
        printf("\n");

        offset += instruction.length;
        instructionPointer += instruction.length;
    }
}