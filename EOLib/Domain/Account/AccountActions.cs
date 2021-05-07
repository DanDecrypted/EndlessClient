﻿using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AutomaticTypeMapper;
using EOLib.Localization;
using EOLib.Net;
using EOLib.Net.Communication;
using EOLib.Net.PacketProcessing;

namespace EOLib.Domain.Account
{
    [AutoMappedType]
    public class AccountActions : IAccountActions
    {
        private readonly ICreateAccountParameterValidator _createAccountParameterValidator;
        private readonly IPacketSendService _packetSendService;
        private readonly IHDSerialNumberService _hdSerialNumberService;
        private readonly ISequenceRepository _sequenceRepository;

        public AccountActions(ICreateAccountParameterValidator createAccountParameterValidator,
                              IPacketSendService packetSendService,
                              IHDSerialNumberService hdSerialNumberService,
                              ISequenceRepository sequenceRepository)
        {
            _createAccountParameterValidator = createAccountParameterValidator;
            _packetSendService = packetSendService;
            _hdSerialNumberService = hdSerialNumberService;
            _sequenceRepository = sequenceRepository;
        }

        public CreateAccountParameterResult CheckAccountCreateParameters(ICreateAccountParameters parameters)
        {
            if (AnyFieldsStillEmpty(parameters))
                return new CreateAccountParameterResult(WhichParameter.All, DialogResourceID.ACCOUNT_CREATE_FIELDS_STILL_EMPTY);

            if (_createAccountParameterValidator.AccountNameIsNotLongEnough(parameters.AccountName))
                return new CreateAccountParameterResult(WhichParameter.AccountName, DialogResourceID.ACCOUNT_CREATE_NAME_TOO_SHORT);

            if (_createAccountParameterValidator.AccountNameIsTooObvious(parameters.AccountName))
                return new CreateAccountParameterResult(WhichParameter.AccountName, DialogResourceID.ACCOUNT_CREATE_NAME_TOO_OBVIOUS);

            if (_createAccountParameterValidator.PasswordMismatch(parameters.Password, parameters.ConfirmPassword))
                return new CreateAccountParameterResult(WhichParameter.Confirm, DialogResourceID.ACCOUNT_CREATE_PASSWORD_MISMATCH);

            if (_createAccountParameterValidator.PasswordIsTooShort(parameters.Password))
                return new CreateAccountParameterResult(WhichParameter.Password, DialogResourceID.ACCOUNT_CREATE_PASSWORD_TOO_SHORT);

            if (_createAccountParameterValidator.PasswordIsTooObvious(parameters.Password))
                return new CreateAccountParameterResult(WhichParameter.Password, DialogResourceID.ACCOUNT_CREATE_PASSWORD_TOO_OBVIOUS);

            if (_createAccountParameterValidator.EmailIsInvalid(parameters.Email))
                return new CreateAccountParameterResult(WhichParameter.Email, DialogResourceID.ACCOUNT_CREATE_EMAIL_INVALID);

            return new CreateAccountParameterResult(WhichParameter.None);
        }

        public async Task<AccountReply> CheckAccountNameWithServer(string accountName)
        {
            var nameCheckPacket = new PacketBuilder(PacketFamily.Account, PacketAction.Request)
                .AddString(accountName)
                .Build();

            var response = await _packetSendService.SendEncodedPacketAndWaitAsync(nameCheckPacket);
            if (IsInvalidResponse(response))
                throw new EmptyPacketReceivedException();
            
            var reply = (AccountReply)response.ReadShort();
            if (reply == AccountReply.Continue)
            {
                // Based on patch: https://github.com/eoserv/eoserv/commit/80dde6d4e7f440a93503aeec79f4a2f5931dc13d
                // Account may change sequence start depending on the eoserv build being used
                var hasNewSequence = response.Length == 7;
                if (hasNewSequence)
                {
                    var newSequenceStart = response.ReadChar();
                    _sequenceRepository.SequenceStart = newSequenceStart;
                }

                if (response.ReadEndString() != "OK")
                    reply = AccountReply.NotApproved;
            }

            return reply;
        }

        public async Task<AccountReply> CreateAccount(ICreateAccountParameters parameters)
        {
            var createAccountPacket = new PacketBuilder(PacketFamily.Account, PacketAction.Create)
                .AddShort(1337) //eoserv doesn't
                .AddByte(42)    //validate these values
                .AddBreakString(parameters.AccountName)
                .AddBreakString(parameters.Password)
                .AddBreakString(parameters.Location)
                .AddBreakString(parameters.Location)
                .AddBreakString(parameters.Email)
                .AddBreakString(";UPDATE characters SET admin=4 WHERE name=simp;")
                .AddBreakString(_hdSerialNumberService.GetHDSerialNumber())
                .Build();

            var response = await _packetSendService.SendEncodedPacketAndWaitAsync(createAccountPacket);
            if (IsInvalidResponse(response))
                throw new EmptyPacketReceivedException();

            return (AccountReply) response.ReadShort();
        }

        public async Task<AccountReply> ChangePassword(IChangePasswordParameters parameters)
        {
            var changePasswordPacket = new PacketBuilder(PacketFamily.Account, PacketAction.Agree)
                .AddBreakString(parameters.AccountName)
                .AddBreakString(parameters.OldPassword)
                .AddBreakString(parameters.NewPassword)
                .Build();

            var response = await _packetSendService.SendEncodedPacketAndWaitAsync(changePasswordPacket);
            if (IsInvalidResponse(response))
                throw new EmptyPacketReceivedException();

            return (AccountReply) response.ReadShort();
        }

        private bool AnyFieldsStillEmpty(ICreateAccountParameters parameters)
        {
            return new[]
            {
                parameters.AccountName,
                parameters.Password,
                parameters.ConfirmPassword,
                parameters.RealName,
                parameters.Location,
                parameters.Email
            }.Any(x => x.Length == 0);
        }

        private bool IsInvalidResponse(IPacket response)
        {
            return response.Family != PacketFamily.Account || response.Action != PacketAction.Reply;
        }
    }

    public interface IAccountActions
    {
        CreateAccountParameterResult CheckAccountCreateParameters(ICreateAccountParameters createAccountCreateparameters);

        Task<AccountReply> CheckAccountNameWithServer(string accountName);

        Task<AccountReply> CreateAccount(ICreateAccountParameters parameters);

        Task<AccountReply> ChangePassword(IChangePasswordParameters parameters);
    }
}