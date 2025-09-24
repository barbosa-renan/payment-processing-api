using AutoMapper;
using PaymentProcessingAPI.Models;
using PaymentProcessingAPI.Models.Entities;

namespace PaymentProcessingAPI.Configurations;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Payment, PaymentResponse>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => Enum.Parse<PaymentStatus>(src.Status)))
            .ForMember(dest => dest.Fees, opt => opt.MapFrom(src => new PaymentFees
            {
                ProcessingFee = src.ProcessingFee,
                GatewayFee = src.GatewayFee,
                TotalFees = src.TotalFees,
                NetAmount = src.NetAmount
            }));

        CreateMap<PaymentRequest, Payment>()
            .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => src.Currency.ToString()))
            .ForMember(dest => dest.PaymentMethod, opt => opt.MapFrom(src => src.PaymentMethod.ToString()))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => PaymentStatus.Processing.ToString()))
            .ForMember(dest => dest.CustomerId, opt => opt.MapFrom(src => src.Customer.CustomerId))
            .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.Customer.Name))
            .ForMember(dest => dest.CustomerEmail, opt => opt.MapFrom(src => src.Customer.Email))
            .ForMember(dest => dest.CustomerDocument, opt => opt.MapFrom(src => src.Customer.Document))
            .ForMember(dest => dest.AddressStreet, opt => opt.MapFrom(src => src.Customer.Address.Street))
            .ForMember(dest => dest.AddressNumber, opt => opt.MapFrom(src => src.Customer.Address.Number))
            .ForMember(dest => dest.AddressComplement, opt => opt.MapFrom(src => src.Customer.Address.Complement))
            .ForMember(dest => dest.AddressNeighborhood, opt => opt.MapFrom(src => src.Customer.Address.Neighborhood))
            .ForMember(dest => dest.AddressCity, opt => opt.MapFrom(src => src.Customer.Address.City))
            .ForMember(dest => dest.AddressState, opt => opt.MapFrom(src => src.Customer.Address.State))
            .ForMember(dest => dest.AddressZipCode, opt => opt.MapFrom(src => src.Customer.Address.ZipCode))
            .ForMember(dest => dest.AddressCountry, opt => opt.MapFrom(src => src.Customer.Address.Country))
            .ForMember(dest => dest.CardHolderName, opt => opt.MapFrom(src => src.Card != null ? src.Card.HolderName : null))
            .ForMember(dest => dest.CardBrand, opt => opt.MapFrom(src => src.Card != null ? src.Card.Brand : null));
    }
}